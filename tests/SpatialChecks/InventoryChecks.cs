using SiteSimulation;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class InventoryChecks
{
    public static int Run(string map, string config, string artPath)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Inventory: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        InventoryItem Item(SpatialSimulation sim, string id) => sim.Inventory.DiagnosticItems.Single(i => i.Id == id);
        InventoryRequest Move(string id, string parent, string region, int x = 0, int y = 0, bool rotated = false) => new(InventoryActionKind.Move, id, new(parent, region, x, y, rotated));
        void Resolve(SpatialSimulation sim)
        {
            for (var i = 0; sim.Inventory.Pending is not null && i < 1000; i++) sim.Tick();
            Check(sim.Inventory.Pending is null, "handling must finish in bounded local time");
        }
        void Act(SpatialSimulation sim, InventoryRequest request)
        { Check(sim.HandleInventory(request), "valid request rejected: " + sim.LastMessage); Resolve(sim); }
        using var art = JsonDocument.Parse(File.ReadAllText(artPath));
        var registered = new HashSet<string>();
        foreach (var asset in art.RootElement.GetProperty("assets").EnumerateArray())
        {
            var id = asset.GetProperty("id").GetString()!; Check(registered.Add(id), "each inventory object has one art binding");
            var bytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(artPath)!, asset.GetProperty("file").GetString()!));
            Check(bytes.Length > 26 && bytes[0] == 137 && bytes[1] == 80 && bytes[25] == 6, "each binding resolves to an RGBA PNG");
            var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            var size = asset.GetProperty("size").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            var r = asset.GetProperty("region").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            Check(width == size[0] && height == size[1], "object art metadata matches original image dimensions");
            Check(r[0] >= 0 && r[1] >= 0 && r[2] > 0 && r[3] > 0 && r[0] + r[2] <= width && r[1] + r[3] <= height,
                "object art registration stays within its real source image");
        }
        var sim = Sim();
        Check(sim.Inventory.Content!.Definitions.All(d => registered.Contains(d.Id)), "all native item definitions have actual object sprites");
        Check(sim.Inventory.Enabled && sim.Inventory.DiagnosticItems.Count == 31, "explicit native profile loads unique physical items");
        Check(sim.InventoryRoots.Count == 1 && sim.InventoryRoots[0].ActorId == sim.Player.Id, "UI roots exclude remote fixtures and other personnel");
        Check(sim.InventoryContents("gum-packet", "pieces").Count == 6, "fine-scale packet contains six individual pieces");
        Check(InventoryHierarchy.Mass(sim.Inventory, "gum-packet") == 15, "recursive packet mass counts the wrapper and each piece once");
        var before = sim.Save();
        var overlap = Move("flashlight", "avatar-trousers", "right");
        Check(!sim.QuoteInventory(overlap).Allowed && !sim.HandleInventory(overlap) && sim.Save() == before, "overlap rejection and preview leave all state untouched");
        Check(!sim.QuoteInventory(Move("flashlight", "gum-packet", "pieces")).Allowed, "coarser icons cannot fit a large object into a fine physical packet");
        Check(!sim.QuoteInventory(Move("medical-case", "medical-case", "inside")).Allowed, "self-containment rejects atomically");
        Check(!sim.QuoteInventory(Move("avatar-backpack", "medical-case", "inside")).Allowed, "ancestor cannot be stored in its descendant");
        Check(!sim.QuoteInventory(Move("flashlight", "person:avatar", "face")).Allowed, "wearable mounts cannot accept arbitrary small items");
        Check(!sim.QuoteInventory(Move("field-phone", "person:tech", "hand")).Allowed, "personal inventory cannot remotely edit another employee");
        Check(!sim.QuoteInventory(Move("field-phone", "fixture:crate-a", "store")).Allowed, "remote stock cannot be transferred without actual reach");
        Check(!sim.QuoteInventory(Move("flashlight", "missing", "inside")).Allowed, "unknown destinations return a reason instead of throwing");
        Check(!sim.QuoteInventory(Move("flashlight", "avatar-coat", "chest", 3, 1)).Allowed, "irregular compartment masks retain their unusable corner");
        Check(!sim.QuoteInventory(Move("field-phone", "avatar-trousers", "left", 1, 0, true)).Allowed, "rotation checks the rotated physical envelope");
        var rotate = Move("flashlight", "avatar-trousers", "left", 0, 1, true);
        Check(sim.QuoteInventory(rotate).Allowed, "a horizontal flashlight fits across the pocket beneath the gum packet");
        var old = Item(sim, "flashlight").Placement;
        Check(sim.HandleInventory(rotate) && Item(sim, "flashlight").Placement == old, "starting a move reserves time without prematurely moving the object");
        sim.Tick();
        Check(sim.Inventory.Pending is { RemainingMs: > 0 } && Item(sim, "flashlight").Placement == old, "partially handled equipment remains at its actual source");
        var midway = sim.Save(); var clone = Sim(); clone.Restore(midway);
        Check(clone.Save() == midway, "inventory work and its source location restore without replay");
        for (var i = 0; i < 50; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Save() == clone.Save() && Item(sim, "flashlight").Placement == rotate.Destination, "rotated timed transfer matches uninterrupted execution exactly");
        Check(sim.Events.Entries.Count(e => e.Text == "Inventory: Flashlight: Move completed.") == 1, "one manipulation produces one completion");
        var pocketQuote = sim.QuoteInventory(Move("flashlight", "person:avatar", "hand"));
        Act(sim, Move("flashlight", "avatar-backpack", "side"));
        var sideQuote = sim.QuoteInventory(Move("flashlight", "person:avatar", "hand"));
        Act(sim, Move("flashlight", "avatar-backpack", "main", 5, 0));
        var mainQuote = sim.QuoteInventory(Move("flashlight", "person:avatar", "hand"));
        Check(pocketQuote.Ap == 2 && sideQuote.Ap == 3 && mainQuote.Ap == 4,
            "pocket, side pocket and main compartment charge distinct data-defined AP");
        Check(pocketQuote.DurationMs < sideQuote.DurationMs && sideQuote.DurationMs < mainQuote.DurationMs,
            "real-time handling shares the same physical access distinctions");
        Act(sim, new(InventoryActionKind.Close, "avatar-backpack", RegionId: "main"));
        Check(sim.InventoryContents("avatar-backpack", "main").Count == 0 && !sim.QuoteInventory(Move("flashlight", "person:avatar", "hand")).Allowed,
            "a closed parent blocks retrieval and hides its live contents");
        Check(sim.QuoteInventory(Move("avatar-backpack", "person:avatar", "hand")).Allowed, "closing the bag does not prevent carrying the bag itself");
        Act(sim, new(InventoryActionKind.Open, "avatar-backpack", RegionId: "main"));
        var nested = Move("dressing-a", "avatar-trousers", "left", 0, 3);
        Check(sim.QuoteInventory(nested).Ap == 6, "nested access includes the outer backpack and the case interior");
        Act(sim, new(InventoryActionKind.Close, "medical-case", RegionId: "inside"));
        Check(!sim.QuoteInventory(nested).Allowed, "closed inner case blocks access even while the outer bag is open");
        Act(sim, Move("medical-case", "avatar-backpack", "main", 0, 4));
        Check(Item(sim, "dressing-a").Placement.ParentId == "medical-case", "moving a full case preserves each descendant's unique identity and parent");
        Check(InventoryHierarchy.Mass(sim.Inventory, "medical-case") == 190, "nested mass remains exact after movement");
        Act(sim, new(InventoryActionKind.Open, "medical-case", RegionId: "inside"));
        Act(sim, nested);
        Check(InventoryHierarchy.Mass(sim.Inventory, "medical-case") == 150, "extracting a child removes its mass from the case exactly once");

        // Authoring variations exercise mass constraints independently of physical size.
        var loadWorld = new SimulationWorld(map, config);
        var definitions = loadWorld.Inventory.Content!.Definitions;
        var trouserIndex = Array.FindIndex(definitions, d => d.Id == "trousers");
        var trousers = definitions[trouserIndex];
        definitions[trouserIndex] = trousers with { Regions = trousers.Regions!.Select(r => r.Id == "left" ? r with { MaxMassGrams = 30 } : r).ToArray() };
        Check(!InventoryActions.Validate(loadWorld, "avatar", Move("dressing-a", "avatar-trousers", "left", 0, 1), out var massReason, out _, out _)
            && massReason.Contains("load limit"), "an item that fits geometrically can still exceed a pocket's recursive load limit");
        var totalWorld = new SimulationWorld(map, config); totalWorld.Player.Cell = new(13, 6);
        var roots = totalWorld.Inventory.Content!.Roots; var own = Array.FindIndex(roots, r => r.ActorId == "avatar");
        roots[own] = roots[own] with { MaxMassGrams = InventoryHierarchy.CarriedMass(totalWorld.Inventory, roots[own].Id) + 999 };
        Check(!InventoryActions.Validate(totalWorld, "avatar", Move("training-weight", "avatar-backpack", "main", 4, 0), out var totalReason, out _, out _)
            && totalReason.Contains("total load"), "a full container's mass also counts against the person's total carrying limit");
        Check(!InventoryActions.Validate(totalWorld, "avatar", Move("training-weight", "avatar-trousers", "left", 0, 1), out var sizeReason, out _, out _)
            && sizeReason.Contains("physically fit"), "thickness prevents bulky objects from fitting in a shallow pocket despite sufficient grid area");

        var phone = Sim();
        Check(phone.OscpAccess(OscpDevice.Phone).Available, "handset in a ready pocket is available");
        Act(phone, Move("field-phone", "avatar-backpack", "main", 4, 0));
        Check(!phone.OscpAccess(OscpDevice.Phone).Available, "main-pack storage removes quick field-phone access even while open");
        Act(phone, Move("flashlight", "avatar-backpack", "main", 0, 4));
        Act(phone, Move("field-phone", "avatar-backpack", "side"));
        Act(phone, new(InventoryActionKind.Close, "avatar-backpack", RegionId: "main"));
        Check(phone.OscpAccess(OscpDevice.Phone).Available, "a ready side pocket stays usable while the separate main compartment is closed");
        Act(phone, Move("field-phone", "avatar-trousers", "right"));
        Act(phone, new(InventoryActionKind.Open, "avatar-backpack", RegionId: "main"));
        Check(phone.OscpAccess(OscpDevice.Phone).Available, "retrieving the actual phone restores its interface");
        Act(phone, new(InventoryActionKind.PowerOff, "field-phone"));
        Check(!phone.OscpAccess(OscpDevice.Phone).Available, "the real item's power state gates the handset service");
        Act(phone, new(InventoryActionKind.PowerOn, "field-phone"));
        Check(phone.OscpAccess(OscpDevice.Phone).Available, "powering the carried device restores access");
        Act(phone, new(InventoryActionKind.Close, "avatar-trousers", RegionId: "right"));
        Check(!phone.OscpAccess(OscpDevice.Phone).Available, "closing an ancestor compartment propagates equipment inaccessibility");
        Act(phone, new(InventoryActionKind.Open, "avatar-trousers", RegionId: "right"));
        var radioWorld = new SimulationWorld(map, config);
        var radio = radioWorld.Inventory.Items["tech-radio"];
        radioWorld.Inventory.Items[radio.Id] = radio with { Placement = new("tech-backpack", "main", 0, 0) };
        Check(!FacilityCommunications.Radio(radioWorld, "tech"), "staff communication uses the employee's actual accessible radio");
        radioWorld.Inventory.Items[radio.Id] = radio;
        Check(FacilityCommunications.Radio(radioWorld, "tech"), "returning staff radio to ready storage restores the existing delivery route");

        var hold = Sim(); Check(hold.HandleInventory(Move("flashlight", "person:avatar", "hand")), "start hold fixture");
        hold.Tick(); var remaining = hold.Inventory.Pending!.RemainingMs; var local = hold.LocalMs; hold.LocalHold = true;
        for (var i = 0; i < 80; i++) hold.Tick();
        Check(hold.LocalMs == local && hold.WorldMs > local && hold.Inventory.Pending!.RemainingMs == remaining, "local hold freezes handling while the calendar advances");
        hold.LocalHold = false; hold.ManualPause = true; var paused = hold.Save();
        for (var i = 0; i < 10; i++) hold.Tick();
        Check(hold.Save() == paused && !hold.HandleInventory(Move("gum-packet", "person:avatar", "hand")), "manual/phone pause cannot supply free packing actions");
        hold.ManualPause = false; Check(hold.CancelInventoryHandling(), "explicit realtime handling cancellation is supported");
        Check(Item(hold, "flashlight").Placement.ParentId == "avatar-backpack", "cancellation leaves the item at source without a teleport or duplication");
        var interrupted = Sim(); interrupted.HandleInventory(Move("flashlight", "person:avatar", "hand")); interrupted.Tick();
        interrupted.Face(new Hex(12, -2).World);
        Check(interrupted.Inventory.Pending is null && Item(interrupted, "flashlight").Placement.ParentId == "avatar-backpack", "an attention change interrupts real-time handling instead of performing both actions");

        var tactical = Sim(); tactical.Player.Facing = 1;
        Check(tactical.ArmTraining() && tactical.Combat.Active, "inventory combat fixture enters actual tactical play");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        tactical.Combat.Roster[tactical.Combat.Cursor] = tactical.Combat.Roster[tactical.Combat.Cursor] with { Ap = 10 };
        var cost = tactical.QuoteInventory(Move("flashlight", "person:avatar", "hand"));
        Check(tactical.HandleInventory(Move("flashlight", "person:avatar", "hand")) && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - cost.Ap,
            "combat charges source-specific AP once when committing manipulation");
        Check(!tactical.HandleInventory(Move("gum-packet", "person:avatar", "hand")), "paid handling forbids overlapping actions");
        tactical.Tick(); var combatSave = tactical.Save(); var combatClone = Sim(); combatClone.Restore(combatSave);
        Check(combatClone.Save() == combatSave, "paid inventory and tactical continuations restore as one schedule");
        for (var i = 0; i < 40; i++) { tactical.Tick(); combatClone.Tick(); }
        Check(tactical.Save() == combatClone.Save() && tactical.Combat.Pending is null && Item(tactical, "flashlight").Placement.ParentId == "person:avatar",
            "paid work advances local time and commits exactly once before input wait returns");
        var afterHandling = tactical.LocalMs;
        for (var i = 0; i < 40; i++) tactical.Tick();
        Check(tactical.LocalMs == afterHandling && tactical.WorldMs > tactical.LocalMs, "viewing inventory at a tactical input hold grants no extra local time");
        var empty = Sim(); empty.Player.Facing = 1; empty.ArmTraining();
        empty.Combat.Cursor = empty.Combat.Roster.FindIndex(c => c.ActorId == empty.Player.Id);
        empty.Combat.Roster[empty.Combat.Cursor] = empty.Combat.Roster[empty.Combat.Cursor] with { Ap = 1 };
        var noAp = empty.Save();
        Check(!empty.HandleInventory(Move("flashlight", "person:avatar", "hand")) && empty.Save() == noAp, "insufficient AP rejects without moving items or spending time");

        var unpaidCombat = JsonNode.Parse(noAp)!;
        unpaidCombat["Inventory"]!["Pending"] = JsonNode.Parse(midway)!["Inventory"]!["Pending"]!.DeepClone();
        try { empty.Restore(unpaidCombat.ToJsonString()); throw new Exception("Unpaid realtime work accepted inside combat"); }
        catch (ArgumentException) { Check(empty.Save() == noAp, "restore cannot inject unpaid realtime work into a tactical input hold"); }

        var nearby = Sim(); nearby.Player.Cell = new(13, 6);
        Check(nearby.InventoryRoots.Any(r => r.PropId == "crate-a"), "nearby fixture view exposes the same existing physical crate");
        Act(nearby, Move("training-weight", "avatar-backpack", "main", 4, 0));
        Check(Item(nearby, "training-weight").Placement.ParentId == "avatar-backpack", "nearby world stock and personal items use the same transfer service");
        var safe = sim.Save();
        void Reject(Action<JsonNode> mutate)
        {
            var bad = JsonNode.Parse(safe)!; mutate(bad);
            try { sim.Restore(bad.ToJsonString()); throw new Exception("Invalid inventory accepted"); }
            catch (ArgumentException) { Check(sim.Save() == safe, "invalid inventory restore must leave the whole running simulation intact"); }
        }
        Reject(n => n["Inventory"]!["Items"]![0]!["Placement"]!["ParentId"] = "missing");
        Reject(n => n["Inventory"]!["Items"]![0]!["DefinitionId"] = "gum");
        Reject(n => n["Inventory"]!["Items"]![0]!["Condition"] = -1);
        Reject(n => n["Inventory"]!["Items"]![0]!["Placement"]!["X"] = 999);
        Reject(n => n["Inventory"]!["Regions"]![0]!["ItemId"] = "missing");
        Reject(n => n.AsObject().Remove("Inventory"));
        var badWork = JsonNode.Parse(midway)!; badWork["Inventory"]!["Pending"]!["CostAp"] = 0;
        var safeWork = clone.Save();
        try { clone.Restore(badWork.ToJsonString()); throw new Exception("Free handling continuation accepted"); }
        catch (ArgumentException) { Check(clone.Save() == safeWork, "invalid work cost cannot change current items or clocks"); }
        var oldDefinition = JsonSerializer.Deserialize<OperationDefinition>(config, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })! with { InventoryProfile = null };
        var legacy = new SpatialSimulation(map, JsonSerializer.Serialize(oldDefinition));
        Check(!legacy.Inventory.Enabled && legacy.OscpAccess(OscpDevice.Phone).Available, "matching legacy scenarios retain their explicit device fixture without invented items");
        Console.WriteLine($"INVENTORY_CHECKS_PASS: {passed} assertions; physical packing, hierarchy, access, timed work, AP, equipment, fixtures and atomic persistence.");
        return passed;
    }
}
