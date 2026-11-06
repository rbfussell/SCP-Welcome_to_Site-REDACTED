using SiteSimulation;
using SitePresentation;
using System.Text.Json;

var mapPath = args.Length > 0 ? args[0] : "game/Content/spatial-facility.json";
var json = File.ReadAllText(mapPath);
var passed = 0;
void Check(bool ok, string message)
{
    if (!ok) throw new Exception(message);
    passed++;
}
void Near(Point2 a, Point2 b, string message) => Check(Point2.Distance(a, b) < 1e-8, message);
SpatialSimulation Sim() => new(json);

// Independent source-derived reference vectors: do not merely test one function against its inverse.
Near(ReferenceProjection.Project(new(1, 0)), new(48, -12), "Floor U must match reference skew.");
Near(ReferenceProjection.Project(new(0, 1)), new(32, 24), "Floor V must match reference skew.");
Near(ReferenceProjection.Project(new Hex(1, 0).World), new(32, 0), "Hex Q center spacing.");
Near(ReferenceProjection.Project(new Hex(0, 1).World), new(16, 12), "Hex R center spacing.");
Near(ReferenceProjection.Project(new Hex(1, -1).World), new(16, -12), "Hex diagonal spacing.");
Check(ReferenceProjection.HexCorners.Max(p => p.X) - ReferenceProjection.HexCorners.Min(p => p.X) == 32
    && ReferenceProjection.HexCorners.Max(p => p.Y) - ReferenceProjection.HexCorners.Min(p => p.Y) == 16, "32×16 reference hex envelope.");
for (var q = -25; q < 25; q++)
for (var r = -25; r < 25; r++)
{
    var h = new Hex(q, r);
    var picked = ReferenceProjection.Unproject(ReferenceProjection.Project(h.World));
    Check(Hex.FromWorld(picked) == h, "Picking must round-trip negative and positive hexes.");
    // Interior points near every reference polygon vertex must remain in this hex.
    foreach (var c in ReferenceProjection.HexCorners)
    {
        var p = ReferenceProjection.Project(h.World) + c * 0.95;
        Check(Hex.FromWorld(ReferenceProjection.Unproject(p)) == h, "Visual hex polygon and input picking disagree.");
    }
}

var map = Sim().Map;
var start = new Hex(9, 2); // corridor, just below lab doorway
var end = new Hex(9, 1); // lab, just above the same doorway
Check(map.FindPath(start, end).Count == 0, "Closed lab door must isolate the room.");
Check(!map.ClearSight(start.World, end.World), "Closed lab door must occlude sight.");
map.OpenDoors.Add("door-4-3-u");
var route = map.FindPath(start, end);
Check(route.Count > 0, "Open lab door must admit a route.");
Check(map.ClearSight(start.World, end.World), "Open doorway must restore sight.");
var previous = start;
foreach (var h in route)
{
    Check(map.CanTraverse(previous, h), "Path contains a wall crossing."); previous = h;
}
// Every allowed step on the whole map must avoid every solid footprint (including corners).
foreach (var a in map.Cells)
foreach (var direction in Enumerable.Range(0, 6))
{
    var b = a.Neighbor(direction);
    if (map.CanTraverse(a, b)) Check(!map.SolidWalls.Any(w => Geometry.Intersects(a.World, b.World, w.Start, w.End)), "Allowed movement crosses a solid wall.");
}

var sim = Sim();
Check(sim.RequestMove(new(12, 3)), "Player needs a clear corridor route.");
sim.Tick(); sim.Tick();
Check(sim.Player.SegmentMs == 100 && sim.Player.Position != sim.Player.Cell.World, "Authoritative position must advance mid-hex.");
var partial = sim.Save();
var clone = Sim(); clone.Restore(partial);
Near(sim.Player.Position, clone.Player.Position, "Save lost fractional movement.");
for (var i = 0; i < 100; i++) { sim.Tick(); clone.Tick(); }
Check(sim.Save() == clone.Save(), "Restored execution diverged, including NPC route/counters.");
var held = Sim(); held.RequestMove(new(12, 3)); held.Tick();
held.LocalHold = true;
var position = held.Player.Position;
var localTime = held.LocalMs;
var timer = held.LocalCountdownMs;
for (var i = 0; i < 240; i++) held.Tick();
Check(held.WorldMs == 12050 && held.OffsiteUpdates == 12, "Other-area/world scheduling must continue during a local hold.");
Check(held.LocalMs == localTime && held.LocalCountdownMs == timer && held.Player.Position == position, "All local progress must remain held.");
Check(!held.RequestMove(new(10, 4)) && !held.ToggleNearestDoor(), "Held UI must not perform local world actions.");
var heldSave = held.Save();
var restoredHold = Sim(); restoredHold.Restore(heldSave);
Check(restoredHold.Save() == heldSave, "Held state failed to restore exactly.");
held.LocalHold = false; held.Tick();
Check(held.LocalMs == localTime + 50 && held.WorldMs == 12100, "Resume must not bulk catch up or double-charge world time.");
held.ManualPause = true;
var beforePause = held.Save();
for (var i = 0; i < 100; i++) held.Tick();
Check(held.Save() == beforePause, "Manual pause must stop all participating state.");

var facing = Sim();
facing.Face(new(1, 4));
var attention = facing.Player.Facing;
Check(facing.RequestMove(new(12, 3), true), "Backward/attention-preserving movement needs a route.");
for (var i = 0; i < 18; i++) facing.Tick();
Check(facing.Player.Facing == attention, "Movement must not forcibly turn an attention-preserving actor.");
var doorSim = Sim(); doorSim.RequestMove(start);
for (var i = 0; i < 200; i++) doorSim.Tick();
Check(doorSim.ToggleNearestDoor(), "Door use should succeed at interaction range.");
Check(doorSim.Map.OpenDoors.Contains("door-4-3-u"), "Wrong door toggled.");
Check(doorSim.RequestMove(end), "Door opening must enable player movement through it.");
for (var i = 0; i < 12; i++) doorSim.Tick();
Check(doorSim.Player.Cell == end, "Player did not cross doorway.");

// Tampering with map identity must fail without touching the running simulation.
var unchanged = sim.Save();
try
{
    var wrongVersion = System.Text.Json.Nodes.JsonNode.Parse(unchanged)!;
    wrongVersion["Version"] = 99;
    sim.Restore(wrongVersion.ToJsonString());
    throw new Exception("Invalid save accepted.");
}
catch (ArgumentException) { Check(sim.Save() == unchanged, "Invalid save partially mutated state."); }
// A future route can legitimately become blocked before the actor reaches it.
var changed = Sim();
changed.Map.OpenDoors.Add("door-4-3-u");
Check(changed.RequestMove(end), "Need an initially open route.");
changed.Map.OpenDoors.Remove("door-4-3-u");
var changedClone = Sim(); changedClone.Restore(changed.Save());
for (var i = 0; i < 150; i++) { changed.Tick(); changedClone.Tick(); }
Check(changed.Save() == changedClone.Save(), "A newly blocked future route must revalidate identically after restore.");
var invalid = System.Text.Json.Nodes.JsonNode.Parse(unchanged)!;
invalid["Actors"]![0]!["Kind"] = "npc";
try { sim.Restore(invalid.ToJsonString()); throw new Exception("Changed avatar kind accepted."); }
catch (ArgumentException) { Check(sim.Save() == unchanged, "Invalid actor identity partially committed."); }
invalid = System.Text.Json.Nodes.JsonNode.Parse(unchanged)!;
invalid["Actors"]![0] = null;
try { sim.Restore(invalid.ToJsonString()); throw new Exception("Null actor accepted."); }
catch (ArgumentException) { Check(sim.Save() == unchanged, "Invalid actor array partially committed."); }
passed += Scp650Checks.Run(json);
passed += ComplacencyChecks.Run(json);
passed += FieldPresentationChecks.Run(json);
passed += AwarenessChecks.Run(json);
passed += LookChecks.Run(json);
passed += PathfindingChecks.Run(json);
passed += OperationChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += EventLogChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += CombatChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += OscpChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")), File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "oscp-apps.json")));
passed += EmploymentChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += InventoryChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")), Path.Combine(Path.GetDirectoryName(mapPath)!, "../Assets/inventory-objects/manifest.json"));
passed += GroundChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += InventoryPackingChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += InventorySecuringChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += InventoryClosureChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += AttachmentChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += JacketBadgeChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += BadgeDoorChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += ShirtBadgeChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += VisiblePocketChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += CoverageChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += BodyFitChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += BodySymmetryChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += BodyDropChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += BodyInspectionChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += UnderwearChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += WearChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += WearFitChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += WornOrderChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += OverlayChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += HookChecks.Run(json, File.ReadAllText(Path.Combine(Path.GetDirectoryName(mapPath)!, "scp650-operation.json")));
passed += SpriteArtChecks.Run(json, Path.Combine(Path.GetDirectoryName(mapPath)!, "../Assets/facility-standins/manifest.json"));
Console.WriteLine($"SPATIAL_CHECKS_PASS: {passed} assertions; projection, picking, wall/door topology, movement, local hold, persistence and facing.");
