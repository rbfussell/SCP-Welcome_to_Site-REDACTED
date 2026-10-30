using SiteSimulation;
using System.Text.Json.Nodes;

internal static class HookChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Hooks: " + reason); passed++; }
        SpatialSimulation Sim() { var s = new SpatialSimulation(map, config); s.Player.Cell = new(13, 6); return s; }
        void Finish(SpatialSimulation s)
        { for (var i = 0; i < 1000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "paid work resolves"); }
        void Act(SpatialSimulation s, InventoryRequest r) { Check(s.HandleInventory(r), s.LastMessage); Finish(s); }
        InventoryRequest Move(string id, string owner, string region, int x = 0, int y = 0)
            => new(InventoryActionKind.Move, id, new(owner, region, x, y));
        InventoryRequest Mount(string id, string owner, string region, BodyPose pose)
            => new(InventoryActionKind.Move, id, new(owner, region, AttachmentPose: pose));
        var sim = Sim(); var state = sim.Inventory;
        var hook = state.Definition("utility-hook");
        Check(hook.Regions is [{ Attachment: not null, Sealable: false }] && hook.DefaultDropRegion is null
            && !state.Regions.Any(r => r.ItemId == "utility-hook"), "extender provides exterior support without a fictitious lid or loose storage");
        Act(sim, Move("utility-belt", "person:avatar", "belt"));
        var attach = Mount("utility-hook", "utility-belt", "webbing", new(0, 0));
        var start = state.Items["utility-hook"].Placement;
        sim.ManualPause = true; var before = sim.Save();
        Check(!sim.HandleInventory(attach) && sim.Save() == before, "paused inspection cannot mount for free"); sim.ManualPause = false;
        Check(sim.HandleInventory(attach) && state.Items["utility-hook"].Placement == start, "paid hook placement preserves source until completion");
        sim.Tick(); var mid = sim.Save(); var copy = Sim(); copy.Restore(mid); Finish(sim); Finish(copy);
        Check(sim.Save() == copy.Save(), "hook mounting resumes exactly across save/restore");
        before = sim.Save(); var drop = sim.PreviewAttachmentDrop("utility-pouch", "utility-hook");
        Check(drop.Handling.Allowed && drop.Request.Destination?.AttachmentPose == new BodyPose(-1, 2)
            && sim.Save() == before, "whole-hook drop targets the offered lower point, not the provider origin or an interior");
        Act(sim, drop.Request);
        Check(state.Items["utility-pouch"].Placement.ParentId == "utility-hook"
            && InventoryAttachmentFrames.RelativePose(state, "utility-pouch", "utility-belt") == new BodyPose(-1, 2), "pouch occupies a composed lower mount with one parent");
        Check(InventoryAttachmentFrames.RelativePose(state, "spare-dressing", "utility-belt") is null, "packed objects never become exterior geometry");
        Act(sim, new(InventoryActionKind.Open, "utility-pouch", RegionId: "inside"));
        Act(sim, Move("spare-dressing", "utility-pouch", "inside"));
        var pouchPlacement = state.Items["utility-pouch"].Placement; var dressingPlacement = state.Items["spare-dressing"].Placement;
        Check(InventoryHierarchy.Mass(state, "utility-belt") == 470, "belt, hook, pouch and payload contribute mass once each");
        Check(InventoryHierarchy.Breadcrumb(state, "spare-dressing").EndsWith("Utility belt / Utility hook / Utility pouch / Dressing packet"), "ordinary hierarchy describes the entire assembly");
        for (var angle = 0; angle < 6; angle++)
        {
            var request = Mount("utility-hook", "utility-belt", "webbing", new(1, 0, angle));
            if (state.Items["utility-hook"].Placement != request.Destination) Act(sim, request);
            // Independent axial reference for the six orientations of the lower point (-1,2).
            BodyHex[] offsets = [new(-1, 2), new(-2, 1), new(-1, -1), new(1, -2), new(2, -1), new(1, 1)];
            var expected = new BodyPose(1 + offsets[angle].Q, offsets[angle].R, angle);
            Check(InventoryAttachmentFrames.RelativePose(state, "utility-pouch", "utility-belt") == expected
                && state.Items["utility-pouch"].Placement == pouchPlacement && state.Items["spare-dressing"].Placement == dressingPlacement,
                "six provider rotations carry exact child coordinates and preserve child identity");
        }
        Act(sim, attach);
        var localHook = state.Items["utility-hook"].Placement;
        Act(sim, Move("utility-belt", "person:avatar", "hand"));
        Check(state.Items["utility-hook"].Placement == localHook && state.Items["utility-pouch"].Placement == pouchPlacement
            && state.Items["spare-dressing"].Placement == dressingPlacement && InventoryAttachments.BodyPosition(state, "utility-pouch") is null,
            "taking off the belt carries both attachment generations and payload without rewriting local placements");
        Act(sim, Move("utility-belt", "person:avatar", "belt"));
        Act(sim, new(InventoryActionKind.Close, "utility-pouch", RegionId: "inside"));
        Check(!sim.InventoryAccess("utility-pouch", "inside").Allowed, "mounted pouch still has its own paid closure");
        var bad = new[] {
            Mount("utility-pouch-2", "utility-hook", "hook", new(0, 0)),
            Mount("field-phone", "utility-hook", "hook", new(-1, 2)),
            new InventoryRequest(InventoryActionKind.AutoPlace, "field-phone", new("utility-hook", "hook")),
            new InventoryRequest(InventoryActionKind.Arrange, "utility-hook", new("utility-hook", "hook")),
            new InventoryRequest(InventoryActionKind.Close, "utility-hook", RegionId: "hook"),
            Mount("utility-hook", "utility-hook", "hook", new(-1, 2))
        };
        foreach (var request in bad)
        { before = sim.Save(); Check(!sim.QuoteInventory(request).Allowed && !sim.HandleInventory(request) && sim.Save() == before, "invalid support/storage/cycle request is atomic"); }
        Check(sim.HandleInventory(Move("utility-hook", "person:avatar", "hand")), "loaded hook detachment starts paid work");
        sim.Tick(); mid = sim.Save(); copy = Sim(); copy.Restore(mid); Finish(sim); Finish(copy);
        Check(copy.Save() == sim.Save(), "loaded nested assembly resumes the same paid detachment");
        Check(state.Items["utility-pouch"].Placement == pouchPlacement && state.Items["spare-dressing"].Placement == dressingPlacement
            && InventoryAttachments.BodyPosition(state, "utility-pouch") is null, "detaching a loaded hook carries its entire assembly");
        Act(sim, Move("utility-hook", "fixture:crate-a", "store", 22, 13));
        sim.Player.Cell = new(9, 2);
        Check(sim.InventoryAttachmentsOn("utility-hook", "hook").Count == 0 && !sim.InventoryAccess("utility-pouch", "inside").Allowed,
            "remote provider cannot disclose its attached gear or contents");

        // Separate worn providers must not bypass the shared body/surface clearance plane.
        var shared = Sim(); Act(shared, Move("avatar-backpack", "fixture:crate-a", "store", 0, 8));
        Act(shared, Move("utility-belt", "person:avatar", "belt"));
        Act(shared, new(InventoryActionKind.Move, "utility-belt-2", new("person:avatar", "belt", BodyPose: new(-4, 10, 2))));
        Act(shared, Mount("utility-hook", "utility-belt", "webbing", new(1, 0)));
        Act(shared, Mount("utility-pouch-2", "utility-belt-2", "webbing", new(-2, 0)));
        var clash = Mount("utility-pouch-2", "utility-belt-2", "webbing", new(1, 0, 3)); before = shared.Save();
        Check(!shared.QuoteInventory(clash).Allowed && shared.QuoteInventory(clash).Reason.Contains("clearance") && shared.Save() == before,
            "different worn providers cannot superimpose attached objects");
        var forged = JsonNode.Parse(before)!;
        var itemNode = forged["Inventory"]!["Items"]!.AsArray().Single(n => n!["Id"]!.GetValue<string>() == "utility-pouch-2")!;
        itemNode["Placement"]!["AttachmentPose"]!["Q"] = 1;
        itemNode["Placement"]!["AttachmentPose"]!["Rotation"] = 3;
        try { shared.Restore(forged.ToJsonString()); throw new Exception("Overlapping saved exterior accepted"); }
        catch (ArgumentException) { Check(shared.Save() == before, "cross-provider collision in a save rejects detached and atomically"); }

        ReferenceClearance(map, config, Check);
        var load = new SimulationWorld(map, config); var ls = load.Inventory;
        ls.Items["utility-hook"] = ls.Items["utility-hook"] with { Placement = new("utility-belt", "webbing", AttachmentPose: new(0, 0)) };
        ls.Items["utility-pouch"] = ls.Items["utility-pouch"] with { Placement = new("utility-hook", "hook", AttachmentPose: new(-1, 2)) };
        ls.Items["spare-dressing"] = ls.Items["spare-dressing"] with { Placement = new("utility-pouch", "inside") };
        var defs = ls.Content!.Definitions; var di = Array.FindIndex(defs, d => d.Id == "dressing");
        defs[di] = defs[di] with { MassGrams = 450 };
        try { InventoryPlacement.Validate(ls); throw new Exception("Overloaded hook accepted"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("Lower hook mount"), "hook load limit includes the loaded descendant pouch"); }

        var tactical = Sim(); Act(tactical, Move("utility-belt", "person:avatar", "belt"));
        Act(tactical, Move("utility-hook", "person:avatar", "hand"));
        tactical.Player.Cell = new(9, 2); tactical.Actors.Single(a => a.Id == "tech").Cell = new(10, 2); tactical.Face(new Hex(10, 2).World);
        Check(tactical.ArmTraining() && tactical.Combat.Active, "tactical fixture starts");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var turn = tactical.Combat.Cursor; var quote = tactical.QuoteInventory(attach);
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = quote.Ap - 1 }; before = tactical.Save();
        Check(!tactical.HandleInventory(attach) && tactical.Save() == before, "hook placement requires the complete quoted AP");
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = 10 };
        Check(tactical.HandleInventory(attach) && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - quote.Ap, "hook AP is charged exactly once");
        Finish(tactical);
        Console.WriteLine($"HOOK_CHECKS_PASS: {passed} assertions; placement extenders, nested mounts, composed rotations, shared clearance, mass, paid AP and persistence.");
        return passed;
    }

    /// <summary>Small authored geometry fixture isolates cousin clearance and connector exceptions from native sprite calibration.</summary>
    private static void ReferenceClearance(string map, string config, Action<bool, string> check)
    {
        var world = new SimulationWorld(map, config); var s = world.Inventory; var defs = s.Content!.Definitions;
        var hi = Array.FindIndex(defs, d => d.Id == "utility-hook"); var hook = defs[hi];
        defs[hi] = hook with { Attachment = new("belt-loop", [new(0, 0), new(0, 1), new(0, 2)], [new(0, 0)]),
            Regions = [hook.Regions![0] with { Attachment = new("belt-loop", [new(0, 2)]) }] };
        var pi = Array.FindIndex(defs, d => d.Id == "utility-pouch");
        defs[pi] = defs[pi] with { Attachment = new("belt-loop", [new(0, 0), new(1, 0)], [new(0, 0)]) };
        s.Items["utility-hook"] = s.Items["utility-hook"] with { Placement = new("utility-belt", "webbing", AttachmentPose: new(0, 0)) };
        s.Items["training-weight"] = s.Items["training-weight"] with { DefinitionId = "utility-hook", Placement = new("utility-belt", "webbing", AttachmentPose: new(2, 0)) };
        s.Items["utility-pouch"] = s.Items["utility-pouch"] with { Placement = new("utility-hook", "hook", AttachmentPose: new(0, 2)) };
        s.Items["utility-pouch-2"] = s.Items["utility-pouch-2"] with { Placement = new("training-weight", "hook", AttachmentPose: new(0, 2)) };
        InventoryPlacement.Validate(s); check(true, "separate hooked pouches meet their parent only at connector contacts");
        s.Items["utility-pouch-2"] = s.Items["utility-pouch-2"] with { Placement = new("training-weight", "hook", AttachmentPose: new(0, 2, 3)) };
        try { InventoryPlacement.Validate(s); throw new Exception("Cousin overlap accepted"); }
        catch (ArgumentException ex) { check(ex.Message.Contains("clearance"), "pouches on different hooks cannot intersect in the common frame"); }
        s.Items["utility-pouch-2"] = s.Items["utility-pouch-2"] with { Placement = new("training-weight", "hook", AttachmentPose: new(0, 2)) };
        // An explicit opposite surface is a separate plane, not a permission to overlap on the same side.
        var wi = Array.FindIndex(defs, d => d.Id == "weight");
        defs[wi] = defs[hi] with { Id = "weight", Regions = [defs[hi].Regions![0] with { Attachment = new("belt-loop", [new(0, 2)], "back") }] };
        s.Items["training-weight"] = s.Items["training-weight"] with { DefinitionId = "weight" };
        s.Items["utility-pouch-2"] = s.Items["utility-pouch-2"] with { Placement = new("training-weight", "hook", AttachmentPose: new(0, 2, 3)) };
        InventoryPlacement.Validate(s); check(true, "explicit opposite surfaces keep separate clearance planes");
        check(InventoryAttachmentFrames.Provider(s, "utility-pouch-2", "mounted").Surface == "back", "nested mounted surfaces inherit the explicit provider side");
        s.Items["training-weight"] = s.Items["training-weight"] with { DefinitionId = "utility-hook" };
        defs[pi] = defs[pi] with { Attachment = new("belt-loop", [new(0, 0), new(0, 1)], [new(0, 0)]) };
        s.Items["utility-pouch-2"] = s.Items["utility-pouch-2"] with { Placement = new("training-weight", "hook", AttachmentPose: new(0, 2)) };
        s.Items["utility-pouch"] = s.Items["utility-pouch"] with { Placement = new("utility-hook", "hook", AttachmentPose: new(0, 2, 3)) };
        try { InventoryPlacement.Validate(s); throw new Exception("Parent silhouette penetration accepted"); }
        catch (ArgumentException ex) { check(ex.Message.Contains("clearance"), "connector exception cannot waive non-contact overlap with the immediate parent"); }
    }
}
