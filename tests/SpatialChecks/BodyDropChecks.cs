using SiteSimulation;

internal static class BodyDropChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Body drop: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        void Finish(SpatialSimulation s)
        { for (var i = 0; i < 2000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "ordinary handling resolves"); }
        void Act(SpatialSimulation s, InventoryRequest request) { Check(s.HandleInventory(request), s.LastMessage); Finish(s); }
        var sim = Sim(); var state = sim.Inventory; var pose = state.BodyFits!.Shape("belt").DefaultPose;
        var before = sim.Save();
        Check(sim.PreviewBodyFitDrop("utility-belt", pose) is null && sim.Save() == before, "out-of-reach stored item exposes no fitting preview");
        Check(sim.PreviewBodyFitDrop("observer-coat", pose) is null, "other employee's gear remains private");
        Check(sim.PreviewBodyFitDrop("missing", pose) is null && sim.PreviewBodyFitDrop("flashlight", pose) is null,
            "unknown items and non-wearables cannot become body fits");
        sim.Player.Cell = new(13, 6); before = sim.Save();
        var fit = sim.PreviewBodyFitDrop("utility-belt", pose)!;
        Check(fit is { Geometry.Fits: true, Handling.Allowed: true } && sim.Save() == before, "explicit accessible stored item previews without moving or scanning");
        Check(sim.PreviewBodyFit("utility-belt", pose) is null && !sim.BodyFitItems.Any(i => i.Id == "utility-belt"),
            "drop query does not broaden the own-equipment selector");
        var direct = new InventoryRequest(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt", BodyPose: pose));
        Check(fit.Request == direct && fit.Handling == sim.QuoteInventory(direct), "drop quotes the exact ordinary request and source-dependent cost");
        var invalid = sim.PreviewBodyFitDrop("utility-belt", new(0, 0))!;
        Check(!invalid.Geometry.Fits && !invalid.Handling.Allowed && !sim.HandleInventory(invalid.Request) && sim.Save() == before,
            "bad geometry refuses without auto-placement or rotation");
        var blocked = sim.PreviewBodyFitDrop("utility-belt", new(-4, 8, 1))!;
        Check(blocked.Geometry.Fits && !blocked.Handling.Allowed && sim.Save() == before, "geometric chest fit cannot pass through the worn outermost pack");
        Check(sim.PreviewBodyFitDrop("utility-belt", pose, "hand") is { Handling.Allowed: false }, "a body pose cannot target a service/hand slot");
        sim.ManualPause = true; before = sim.Save();
        Check(!sim.HandleInventory(fit.Request) && sim.Save() == before, "paused drop cannot grant free dressing");
        sim.ManualPause = false;
        var source = state.Items["utility-belt"].Placement;
        Check(sim.HandleInventory(fit.Request) && state.Items["utility-belt"].Placement == source, "source remains in storage while fitting is paid");
        sim.Tick(); var pending = sim.Save(); var clone = Sim(); clone.Restore(pending);
        Check(clone.Save() == pending, "pending direct stored-to-body fit restores exactly"); Finish(sim); Finish(clone);
        Check(sim.Save() == clone.Save() && state.Items["utility-belt"].Placement.BodyPose == pose,
            "direct stored fitting commits the previewed anchor and continues identically");

        var nested = Sim(); nested.Player.Cell = new(13, 6);
        Act(nested, new(InventoryActionKind.AutoPlace, "utility-belt", new("avatar-backpack", "main")));
        Check(nested.PreviewBodyFitDrop("utility-belt", pose) is { Handling.Allowed: true }, "accessible open-parent clothing can be fitted directly");
        Act(nested, new(InventoryActionKind.Close, "avatar-backpack", RegionId: "main")); before = nested.Save();
        Check(nested.PreviewBodyFitDrop("utility-belt", pose) is null && nested.Save() == before, "closed ancestor reveals no drop geometry");
        Act(nested, new(InventoryActionKind.Open, "avatar-backpack", RegionId: "main"));
        var nestedFit = nested.PreviewBodyFitDrop("utility-belt", pose)!;
        Check(nested.HandleInventory(nestedFit.Request), nested.LastMessage);
        var regionIndex = nested.Inventory.Regions.FindIndex(r => r.ItemId == "avatar-backpack" && r.RegionId == "main");
        nested.Inventory.Regions[regionIndex] = nested.Inventory.Regions[regionIndex] with { Open = false };
        Finish(nested);
        Check(nested.Inventory.Items["utility-belt"].Placement.ParentId == "avatar-backpack", "post-start access loss aborts at commit instead of moving through a closed ancestor");

        var front = Sim(); front.Player.Cell = new(13, 6);
        Act(front, new(InventoryActionKind.Move, "spare-backpack", new("person:avatar", "front")));
        var packPose = front.Inventory.BodyFits!.Shape("backpack").DefaultPose;
        Check(front.PreviewBodyFitDrop("spare-backpack", packPose)!.Request.Destination!.RegionId == "front", "refit preserves an existing front mount");

        var tactical = Sim(); tactical.Player.Cell = new(13, 6);
        Act(tactical, new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "hand")));
        tactical.Player.Cell = new(9, 2); tactical.Actors.Single(a => a.Id == "tech").Cell = new(10, 2); tactical.Face(new Hex(10, 2).World);
        Check(tactical.ArmTraining() && tactical.Combat.Active, "tactical fixture starts");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var turn = tactical.Combat.Cursor; var tacticalFit = tactical.PreviewBodyFitDrop("utility-belt", pose)!;
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = tacticalFit.Handling.Ap - 1 }; before = tactical.Save();
        Check(!tactical.HandleInventory(tacticalFit.Request) && tactical.Save() == before, "insufficient AP refuses a body drop atomically");
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = 10 };
        Check(tactical.HandleInventory(tacticalFit.Request) && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - tacticalFit.Handling.Ap,
            "body drop spends normal AP once"); Finish(tactical);
        Check(tactical.Inventory.Items["utility-belt"].Placement.BodyPose == pose, "tactical fit commits normally");
        Console.WriteLine($"BODY_DROP_CHECKS_PASS: {passed} assertions; explicit accessible source, no hidden scan, exact pose/cost, blockers, pause, AP, commit revalidation and continuation.");
        return passed;
    }
}
