using SiteSimulation;

internal static class BodyInspectionChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Body inspection: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        void Finish(SpatialSimulation s) { for (var i = 0; i < 2000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "ordinary paid work resolves"); }
        void Act(SpatialSimulation s, InventoryRequest r) { Check(s.HandleInventory(r), s.LastMessage); Finish(s); }
        var sim = Sim(); var chest = new BodyHex(-4, 8); var before = sim.Save();
        Check(sim.InventoryBodyPick("front", chest)?.ItemId == "avatar-shirt", "open jacket exposes the shirt at its opening");
        Check(sim.InventoryBodyPick("back", chest)?.ItemId == "avatar-backpack", "back-mounted pack wins on the back surface only");
        Check(sim.InventoryBodyPick("front", new(-2, 4))?.ItemId == "avatar-coat", "retained jacket shoulder material stays on top");
        Check(sim.InventoryBodyPick("front", new(0, 0)) is null && sim.InventoryBodyPick("front", new(40, 40)) is null,
            "bare body and outside positions are not equipment targets");
        Check(sim.InventoryBodySurface("other").Count == 0 && sim.InventoryBodySurface("mounted").Count == 0, "only explicit front/back surfaces are supported");
        foreach (var surface in new[] { "front", "back" })
        {
            var targets = sim.InventoryBodySurface(surface);
            Check(targets.SelectMany(t => t.Cells).Distinct().Count() == targets.Sum(t => t.Cells.Length), "exposed target cells never overlap: " + surface);
            Check(targets.All(t => sim.Inventory.Items[t.ItemId].Placement.ParentId == "person:avatar" && t.Cells.Length > 0), "only own worn instances are projected: " + surface);
            Check(!targets.Any(t => t.ItemId is "medical-case" or "flashlight" or "chemical-suit" or "observer-coat"), "no contents, stored or foreign gear leaks: " + surface);
        }
        Check(sim.Save() == before, "projection, picking and access feedback are read-only");
        var shirt = sim.InventoryBodyPick("front", chest)!;
        Check(!shirt.MoveAccess.Allowed && shirt.MoveAccess == sim.InventoryMoveAccess("avatar-shirt"), "visible material does not grant removal through an outer pack");
        Act(sim, new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(sim.InventoryBodyPick("front", chest)?.ItemId == "avatar-coat", "closing changes the actual top target after paid completion");
        Check(!sim.InventoryBodySurface("front").Any(t => t.ItemId == "avatar-shirt"), "fully hidden shirt has no front hit region");
        Act(sim, new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Check(sim.InventoryBodyPick("front", chest)?.ItemId == "avatar-shirt", "reopening restores the exposed lower target");
        sim.Player.Cell = new(13, 6);
        Act(sim, new(InventoryActionKind.Move, "spare-backpack", new("person:avatar", "front")));
        Check(sim.InventoryBodyPick("front", chest)?.ItemId == "spare-backpack" && sim.InventoryBodyPick("back", chest)?.ItemId == "avatar-backpack",
            "two packs are distinct items on distinct surfaces");
        Act(sim, new(InventoryActionKind.Move, "spare-backpack", new("fixture:crate-a", "store", 13, 0)));
        Act(sim, new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        var waist = new BodyHex(-6, 12);
        Check(sim.InventoryBodyPick("front", waist)?.ItemId == "utility-belt" && sim.InventoryBodyPick("back", waist)?.ItemId == "utility-belt",
            "wrapping belt is the same source identity on both surfaces");

        var packSource = sim.InventoryBodyPick("back", chest)!.ItemId;
        var casePlacement = sim.Inventory.Items["medical-case"].Placement; var lightPlacement = sim.Inventory.Items["flashlight"].Placement;
        var mass = InventoryHierarchy.CarriedMass(sim.Inventory, "person:avatar");
        var remove = new InventoryRequest(InventoryActionKind.Move, packSource, new("person:avatar", "hand"));
        sim.ManualPause = true; before = sim.Save();
        Check(!sim.HandleInventory(remove) && sim.Save() == before, "paused body-source move cannot undress for free");
        sim.ManualPause = false;
        Check(sim.HandleInventory(remove) && sim.InventoryBodyPick("back", chest)?.ItemId == packSource, "source remains worn until paid completion");
        sim.Tick(); var midway = sim.Save(); var clone = Sim(); clone.Restore(midway);
        Check(clone.Save() == midway, "paid body-source move restores exactly"); Finish(sim); Finish(clone);
        Check(sim.Save() == clone.Save(), "body-source continuation is deterministic");
        Check(sim.InventoryBodyPick("back", chest)?.ItemId == "avatar-coat" && !sim.InventoryBodySurface("back").Any(t => t.ItemId == packSource),
            "held pack leaves the worn projection and exposes the jacket");
        Check(sim.Inventory.Items["medical-case"].Placement == casePlacement && sim.Inventory.Items["flashlight"].Placement == lightPlacement
            && InventoryHierarchy.CarriedMass(sim.Inventory, "person:avatar") == mass, "removing a loaded provider preserves child placement, identity and counted mass");

        var tactical = Sim(); tactical.Player.Cell = new(13, 6);
        Act(tactical, new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        tactical.Player.Cell = new(9, 2); tactical.Actors.Single(a => a.Id == "tech").Cell = new(10, 2); tactical.Face(new Hex(10, 2).World);
        Check(tactical.ArmTraining(), "tactical fixture starts");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var move = new InventoryRequest(InventoryActionKind.Move, tactical.InventoryBodyPick("front", waist)!.ItemId, new("avatar-backpack", "main", 0, 3));
        var quote = tactical.QuoteInventory(move); Check(quote.Allowed, quote.Reason);
        var turn = tactical.Combat.Cursor;
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = quote.Ap - 1 }; before = tactical.Save();
        Check(!tactical.HandleInventory(move) && tactical.Save() == before, "body-to-storage move refuses insufficient AP atomically");
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = 10 };
        Check(tactical.HandleInventory(move) && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - quote.Ap, "body-source action spends normal AP once"); Finish(tactical);
        Check(tactical.Inventory.Items["utility-belt"].Placement == new ItemPlacement("avatar-backpack", "main", 0, 3)
            && tactical.InventoryBodyPick("front", waist)?.ItemId == "avatar-trousers", "exact storage move clears worn coordinates/order and reveals the trousers");
        Console.WriteLine($"BODY_INSPECTION_CHECKS_PASS: {passed} assertions; exposed own surfaces, open covers, front/back packs, no hidden contents, paid removal, AP and continuation.");
        return passed;
    }
}
