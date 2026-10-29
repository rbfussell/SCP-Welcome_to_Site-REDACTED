using SiteSimulation;
using System.Text.Json.Nodes;

internal static class AttachmentChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Attachments: " + reason); passed++; }
        SpatialSimulation Sim() { var s = new SpatialSimulation(map, config); s.Player.Cell = new(13, 6); return s; }
        void Finish(SpatialSimulation s) { for (var i = 0; i < 1000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "paid work resolves"); }
        void Act(SpatialSimulation s, InventoryRequest request) { Check(s.HandleInventory(request), "request rejected: " + s.LastMessage); Finish(s); }
        InventoryRequest Move(string id, string parent, string region) => new(InventoryActionKind.Move, id, new(parent, region));
        InventoryRequest Mount(SpatialSimulation s, int q = 0, int r = 0, int angle = 0) => s.PreviewAttachment("utility-pouch", "utility-belt", "webbing", new(q, r, angle)).Request;
        var sim = Sim(); var state = sim.Inventory;
        Check(state.Region("utility-belt", "webbing").Attachment is not null && state.IsOpen("utility-belt", "webbing"), "support is an always-accessible exterior, not a closable pocket");
        Check(!state.Regions.Any(r => r.ItemId == "utility-belt"), "no fictitious belt-lid state");
        var shape = state.BodyFits!.Shape("belt"); var body = state.BodyFits.Body(shape.BodyFamily);
        Check(BodyFitGeometry.Evaluate(body, shape, shape.DefaultPose).Fits, "belt ends fit waist");
        Check(BodyFitGeometry.Evaluate(body, shape, new(-3, 9, 2)).Fits, "transformed end contacts also admit a diagonal torso fit");
        Check(!BodyFitGeometry.Evaluate(body, shape, new(-5, 9)).Fits, "interior endpoints are not allowed to float without body-edge contact");
        Act(sim, Move("utility-belt", "person:avatar", "belt"));
        for (var angle = 0; angle < 6; angle++)
            Check(sim.QuoteInventory(Mount(sim, angle: angle)).Allowed, "six mounting directions are independent of square-storage rotation");
        var unchangedDrop = sim.Save();
        var automatic = sim.PreviewAttachmentDrop("utility-pouch", "utility-belt", 2);
        Check(automatic.Handling.Allowed && automatic.Request.Destination?.AttachmentPose == new BodyPose(0, 0, 2) && sim.Save() == unchangedDrop,
            "whole-provider drop chooses deterministic free support and preserves the requested orientation without mutating state");
        Check(!sim.PreviewAttachmentDrop("field-phone", "utility-belt").Handling.Allowed
            && !sim.PreviewAttachmentDrop("utility-pouch", "utility-belt", 6).Handling.Allowed, "whole-provider drop rejects incompatible items and invalid directions");
        var source = state.Items["utility-pouch"].Placement; var quote = sim.QuoteInventory(Mount(sim));
        Check(quote.DurationMs > 0 && quote.Ap > 0, "mounting quotes real handling time and AP");
        sim.ManualPause = true; var safe = sim.Save();
        Check(!sim.HandleInventory(Mount(sim)) && sim.Save() == safe, "pause cannot grant a free attachment"); sim.ManualPause = false;
        Check(sim.HandleInventory(Mount(sim)) && state.Items["utility-pouch"].Placement == source, "mount begins without teleporting the item"); sim.Tick();
        var mid = sim.Save(); var copy = Sim(); copy.Restore(mid); Check(copy.Save() == mid, "mid-attachment continuation restores exactly");
        Finish(sim); Finish(copy); Check(sim.Save() == copy.Save(), "resumption matches uninterrupted attachment");
        var attached = state.Items["utility-pouch"].Placement;
        unchangedDrop = sim.Save();
        Check(!sim.PreviewAttachmentDrop("utility-pouch", "utility-belt").Handling.Allowed && sim.Save() == unchangedDrop,
            "dropping onto the existing provider does not relocate or rearrange its mounted child");
        Check(attached.ParentId == "utility-belt" && attached.AttachmentPose == new BodyPose(0, 0), "single physical parent plus local mount pose");
        Check(InventoryAttachments.BodyPosition(state, "utility-pouch") == shape.DefaultPose, "mount anchor resolves through worn support");
        Check(sim.InventoryContents("utility-pouch", "inside").Count == 0 && !sim.InventoryAccess("utility-pouch", "inside").Allowed,
            "mounting does not open the pouch or disclose contents");
        Act(sim, new(InventoryActionKind.Open, "utility-pouch", RegionId: "inside"));
        Act(sim, Move("spare-dressing", "utility-pouch", "inside"));
        var dressing = state.Items["spare-dressing"].Placement;
        Check(InventoryHierarchy.Mass(state, "utility-belt") == 390, "loaded parent mass counts belt, pouch and dressing once");
        Check(InventoryHierarchy.Breadcrumb(state, "spare-dressing").EndsWith("Utility belt / Utility pouch / Dressing packet"), "attachment ownership shares the ordinary hierarchy");
        Act(sim, Move("utility-belt", "person:avatar", "hand"));
        Check(state.Items["utility-pouch"].Placement == attached && state.Items["spare-dressing"].Placement == dressing
            && InventoryAttachments.BodyPosition(state, "utility-pouch") is null, "taking belt off carries its loaded attachment without rewriting children");
        var fit = new InventoryRequest(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt", BodyPose: new(-3, 9, 2)));
        Act(sim, fit);
        Check(InventoryAttachments.BodyPosition(state, "utility-pouch") == new BodyPose(-3, 9, 2), "rotating/translating belt composes child's body pose");
        Act(sim, Mount(sim, 1, 0, 4));
        Check(InventoryAttachments.BodyPosition(state, "utility-pouch") == new BodyPose(-4, 10, 0), "independent axial reference: parent 120 plus child 240 equals full turn");
        Check(sim.InventoryAccess("avatar-trousers", "left").Allowed && sim.InventoryAccess("avatar-trousers", "cargo-left").Allowed,
            "belt leaves ordinary pockets accessible");
        Check(!sim.InventoryMoveAccess("avatar-trousers").Allowed && !sim.InventoryMoveAccess("avatar-shirt").Allowed,
            "belt restrains removal independently of access");
        var invalid = new[] {
            Mount(sim, 9), Mount(sim, angle: 6), Mount(sim, angle: -1), Mount(sim, r: int.MaxValue),
            new InventoryRequest(InventoryActionKind.Move, "field-phone", new("utility-belt", "webbing", AttachmentPose: new(0, 0))),
            new InventoryRequest(InventoryActionKind.Move, "utility-pouch", new("utility-belt", "webbing")),
            Mount(sim) with { Destination = new("utility-belt", "webbing", Rotated: true, AttachmentPose: new(0, 0)) },
            Mount(sim) with { Destination = new("utility-belt", "webbing", X: 1, AttachmentPose: new(0, 0)) },
            Move("utility-pouch", "avatar-backpack", "main") with { Destination = new("avatar-backpack", "main", AttachmentPose: new(0, 0)) },
            new InventoryRequest(InventoryActionKind.Arrange, "utility-belt", new("utility-belt", "webbing")),
            new InventoryRequest(InventoryActionKind.AutoPlace, "field-phone", new("utility-belt", "webbing")),
            new InventoryRequest(InventoryActionKind.Close, "utility-belt", RegionId: "webbing"),
            Move("utility-belt", "utility-pouch", "inside")
        };
        foreach (var request in invalid)
        { safe = sim.Save(); Check(!sim.QuoteInventory(request).Allowed && !sim.HandleInventory(request) && sim.Save() == safe, "invalid mount/packing/cycle leaves graph and clocks untouched: " + request); }
        Act(sim, new(InventoryActionKind.Close, "utility-pouch", RegionId: "inside"));
        Check(!sim.QuoteInventory(Move("spare-dressing", "person:avatar", "hand")).Allowed, "pouch closure gates child access on belt");
        Act(sim, new(InventoryActionKind.Open, "utility-pouch", RegionId: "inside"));
        var loadedMass = InventoryHierarchy.CarriedMass(state, "person:avatar");
        Act(sim, Move("utility-pouch", "person:avatar", "hand"));
        Check(state.Items["utility-pouch"].Placement.AttachmentPose is null && state.Items["spare-dressing"].Placement == dressing
            && InventoryHierarchy.CarriedMass(state, "person:avatar") == loadedMass, "detaching keeps contents and total carried mass");
        Act(sim, Mount(sim));
        Act(sim, Move("utility-belt", "fixture:crate-a", "store") with { Destination = new("fixture:crate-a", "store", 0, 13) });
        sim.Player.Cell = new(9, 2);
        Check(!sim.InventoryAccess("utility-pouch", "inside").Allowed && sim.InventoryAttachmentsOn("utility-belt", "webbing").Count == 0, "remote belt does not expose attached contents");

        // Validate independently malformed authored interfaces, overlap and recursive load limits.
        var world = new SimulationWorld(map, config); world.Player.Cell = new(13, 6); var s = world.Inventory;
        s.Items["utility-pouch"] = s.Items["utility-pouch"] with { Placement = new("utility-belt", "webbing", AttachmentPose: new(0, 0)) };
        InventoryPlacement.Validate(s);
        var defs = s.Content!.Definitions; var pi = Array.FindIndex(defs, d => d.Id == "utility-pouch"); var pouch = defs[pi];
        void RejectGraph(string reason) { try { InventoryPlacement.Validate(s); throw new Exception("Accepted: " + reason); } catch (ArgumentException) { Check(true, reason); } }
        defs[pi] = pouch with { Attachment = pouch.Attachment! with { Interface = "other-interface" } }; RejectGraph("wrong connector refused"); defs[pi] = pouch;
        s.Items["spare-dressing"] = s.Items["spare-dressing"] with { DefinitionId = "utility-pouch", Placement = new("utility-belt", "webbing", AttachmentPose: new(0, 0, 3)) };
        RejectGraph("occupied anchor cannot accept an overlapping attachment");
        s.Items["spare-dressing"] = s.Items["spare-dressing"] with { Placement = new("utility-belt", "webbing", AttachmentPose: new(1, 0)) };
        RejectGraph("upright pouch width occupies both lower neighbors, preventing side-by-side silhouette overlap");
        s.Items["spare-dressing"] = s.Items["spare-dressing"] with { Placement = new("utility-belt", "webbing", AttachmentPose: new(2, 0)) };
        InventoryPlacement.Validate(s); Check(true, "separate support positions do not overlap");
        var bi = Array.FindIndex(defs, d => d.Id == "utility-belt"); var belt = defs[bi];
        defs[bi] = belt with { Regions = belt.Regions!.Select(r => r with { MaxMassGrams = 150 }).ToArray() }; RejectGraph("support sums all loaded attachments"); defs[bi] = belt;
        Check(!InventoryAttachments.ProfileValid(pouch.Attachment! with { Contacts = [new(99, 99)] })
            && !InventoryAttachments.ProfileValid(pouch.Attachment! with { Occupancy = [new(0, 0), new(0, 0)] }), "malformed authored footprints rejected");

        var blockedDrop = Sim();
        blockedDrop.Inventory.Items["utility-belt"] = blockedDrop.Inventory.Items["utility-belt"] with { Placement = new("avatar-backpack", "main", 0, 4) };
        var closedIndex = blockedDrop.Inventory.Regions.FindIndex(r => r.ItemId == "avatar-backpack" && r.RegionId == "main");
        blockedDrop.Inventory.Regions[closedIndex] = blockedDrop.Inventory.Regions[closedIndex] with { Open = false };
        unchangedDrop = blockedDrop.Save();
        Check(!blockedDrop.PreviewAttachmentDrop("utility-pouch", "utility-belt").Handling.Allowed && blockedDrop.Save() == unchangedDrop,
            "whole-provider shortcut respects closed ancestors and performs no hidden opening");
        var drop = Sim(); Act(drop, Move("utility-belt", "person:avatar", "belt"));
        var dropRequest = drop.PreviewAttachmentDrop("utility-pouch", "utility-belt", 5).Request;
        Act(drop, dropRequest);
        Check(drop.Inventory.Items["utility-pouch"].Placement.AttachmentPose == new BodyPose(0, 0, 5), "provider drop commits the quoted sixth-turn pose through paid work");

        var offset = Sim(); var offsetDefs = offset.Inventory.Content!.Definitions;
        var offsetBelt = Array.FindIndex(offsetDefs, d => d.Id == "utility-belt");
        offsetDefs[offsetBelt] = offsetDefs[offsetBelt] with { Regions = offsetDefs[offsetBelt].Regions!.Select(r => r with
            { Attachment = r.Attachment! with { Cells = [new(0, 0)] } }).ToArray() };
        var offsetPouch = Array.FindIndex(offsetDefs, d => d.Id == "utility-pouch");
        offsetDefs[offsetPouch] = offsetDefs[offsetPouch] with { Attachment = new("belt-loop", [new(1, 0), new(1, 1)], [new(1, 0)]) };
        var offsetPreview = offset.PreviewAttachmentDrop("utility-pouch", "utility-belt", 2);
        Check(offsetPreview.Handling.Allowed && offsetPreview.Request.Destination?.AttachmentPose == new BodyPose(1, -1, 2),
            "non-origin contacts align with support at the requested direction instead of requiring the item's origin on a support cell");
        var dd = Array.FindIndex(offsetDefs, d => d.Id == "dressing");
        offsetDefs[dd] = offsetDefs[dd] with { Attachment = new("belt-loop", [new(0, 0)], [new(0, 0)]) };
        offset.Inventory.Items["spare-dressing"] = offset.Inventory.Items["spare-dressing"] with { Placement = new("utility-belt", "webbing", AttachmentPose: new(0, 0)) };
        unchangedDrop = offset.Save();
        Check(!offset.PreviewAttachmentDrop("utility-pouch", "utility-belt", 2).Handling.Allowed && offset.Save() == unchangedDrop,
            "a full exterior support never rearranges or rotates existing mounted equipment");

        var accessWorld = new SimulationWorld(map, config); accessWorld.Player.Cell = new(13, 6); var aw = accessWorld.Inventory;
        aw.Items["utility-belt"] = aw.Items["utility-belt"] with { Placement = new("person:avatar", "belt", WornOrder: 7) };
        foreach (var id in new[] { "coat", "backpack" })
        {
            var index = Array.FindIndex(aw.Content!.Definitions, d => d.Id == id); var def = aw.Content.Definitions[index];
            aw.Content.Definitions[index] = def with { Wear = def.Wear! with { CoversAccess = false, BlocksRemoval = false } };
        }
        Check(!InventoryActions.Validate(accessWorld, "avatar", Move("avatar-shirt", "person:avatar", "hand"), out var restraint, out _, out _)
            && restraint.Contains("Utility belt"), "belt alone restrains shirt removal without relying on coat or backpack");
        aw.Items["utility-belt"] = aw.Items["utility-belt"] with { Placement = new("avatar-backpack", "main", 0, 4) };
        aw.Items["utility-pouch"] = aw.Items["utility-pouch"] with { Placement = new("utility-belt", "webbing", AttachmentPose: new(0, 0)) };
        var mainIndex = aw.Regions.FindIndex(r => r.ItemId == "avatar-backpack" && r.RegionId == "main");
        aw.Regions[mainIndex] = aw.Regions[mainIndex] with { Open = false };
        Check(!InventoryAccessService.Placement(accessWorld, "avatar", aw.Items["utility-pouch"].Placement).Allowed,
            "closed enclosing storage gates an attachment even though its support has no lid");
        Check(InventoryActions.Validate(accessWorld, "avatar", Move("avatar-shirt", "person:avatar", "hand"), out _, out _, out _),
            "packed belt no longer restrains worn clothing");
        var changing = Sim(); Check(changing.HandleInventory(Mount(changing)), "begin before support becomes inaccessible");
        changing.Inventory.Items["utility-belt"] = changing.Inventory.Items["utility-belt"] with { Placement = new("avatar-backpack", "main", 0, 4) };
        var changingMain = changing.Inventory.Regions.FindIndex(r => r.ItemId == "avatar-backpack" && r.RegionId == "main");
        changing.Inventory.Regions[changingMain] = changing.Inventory.Regions[changingMain] with { Open = false }; Finish(changing);
        Check(changing.Inventory.Items["utility-pouch"].Placement.ParentId == "fixture:crate-a", "completion revalidates support ancestry and preserves the source on access loss");

        var cancel = Sim(); Check(cancel.HandleInventory(Mount(cancel)), "begin attach for cancellation"); cancel.CancelInventoryHandling();
        Check(cancel.Inventory.Items["utility-pouch"].Placement.ParentId == "fixture:crate-a", "cancellation never commits attachment");
        var tactical = Sim(); Act(tactical, Move("utility-belt", "person:avatar", "belt"));
        Act(tactical, Move("utility-pouch", "person:avatar", "hand"));
        tactical.Player.Cell = new(9, 2); tactical.Actors.Single(a => a.Id == "tech").Cell = new(10, 2);
        tactical.Face(new Hex(10, 2).World); Check(tactical.ArmTraining() && tactical.Combat.Active, "tactical fixture starts actual contact");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var turn = tactical.Combat.Cursor; tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = 10 };
        var cost = tactical.QuoteInventory(Mount(tactical));
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = cost.Ap - 1 }; safe = tactical.Save();
        Check(!tactical.HandleInventory(Mount(tactical)) && tactical.Save() == safe, "insufficient AP does not buy partial mounting");
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = 10 };
        var committed = tactical.HandleInventory(Mount(tactical));
        Check(committed && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - cost.Ap, "mount consumes quoted AP in tactical combat: " + tactical.LastMessage); Finish(tactical);
        var time = tactical.LocalMs; tactical.Tick(); Check(tactical.LocalMs == time, "attachment returns to tactical decision hold");

        void RejectSave(Action<JsonNode> mutate)
        {
            var victim = Sim(); var before = victim.Save(); var bad = JsonNode.Parse(mid)!; mutate(bad);
            try { victim.Restore(bad.ToJsonString()); throw new Exception("Forged attachment save accepted"); }
            catch (ArgumentException) { Check(victim.Save() == before, "restore rejection preserves live world"); }
        }
        RejectSave(n => n["Inventory"]!["Pending"]!["Request"]!["Destination"]!["AttachmentPose"]!["Rotation"] = 6);
        RejectSave(n => n["Inventory"]!["Pending"]!["CostAp"] = 0);
        RejectSave(n => n["Inventory"]!["Items"]!.AsArray().First(i => i!["Id"]!.GetValue<string>() == "field-phone")!["Placement"]!["AttachmentPose"] = new JsonObject { ["Q"] = 0, ["R"] = 0, ["Rotation"] = 0 });
        Console.WriteLine($"ATTACHMENT_CHECKS_PASS: {passed} assertions; local hex mounts, interfaces, collision, recursive loads, paid actions, restraints and persistence.");
        return passed;
    }
}
