using SiteSimulation;

internal static class ShirtBadgeChecks
{
    public static int Run(string map, string config)
    {
        var count = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Shirt/badge: " + reason); count++; }
        var sim = new SpatialSimulation(map, config); sim.Player.Cell = new(13, 6);
        void Finish() { for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) sim.Tick(); Check(sim.Inventory.Pending is null, "paid work finishes"); }
        void Act(InventoryRequest r) { Check(sim.HandleInventory(r), sim.LastMessage); Finish(); }
        var pockets = new[] { "chest-left", "chest-right" };
        foreach (var id in pockets)
        {
            Check(!sim.Inventory.IsOpen("avatar-shirt", id) && sim.InventoryPocketCoverage("avatar-shirt", id)?.Covered == false,
                "two independent closed pockets have exposed mouths under open jacket");
            Act(new(InventoryActionKind.Open, "avatar-shirt", RegionId: id));
        }
        Act(new(InventoryActionKind.AutoPlace, "spare-dressing", new("avatar-shirt", "chest-right")));
        Check(!sim.QuoteInventoryPacking(new(InventoryActionKind.AutoPlace, "field-phone", new("avatar-shirt", "chest-left"))).Allowed,
            "shirt pockets do not become backpack-sized storage");
        var attach = new InventoryRequest(InventoryActionKind.Move, "personnel-badge", new("avatar-shirt", "badge-clip", AttachmentPose: new(0, 2)));
        sim.ManualPause = true; var before = sim.Save(); Check(!sim.HandleInventory(attach) && sim.Save() == before, "paused badge mounting refuses");
        sim.ManualPause = false; Check(sim.HandleInventory(attach), sim.LastMessage);
        Check(sim.Inventory.Pending is { CostAp: > 0, TotalMs: > 0 } && sim.Inventory.DiagnosticItems.Single(i => i.Id == "personnel-badge").Placement.ParentId == "spare-backpack", "badge remains at source while paid mounting resolves");
        var pending = sim.Save(); var clone = new SpatialSimulation(map, config); clone.Restore(pending); Check(clone.Save() == pending, "badge pending continuation is exact"); Finish();
        Check(sim.InventoryMoveAccess("personnel-badge").Allowed, "exposed badge is accessible");
        Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(!sim.InventoryMoveAccess("personnel-badge").Allowed, "covered attached badge cannot be accessed through jacket");
        Check(pockets.All(id => sim.InventoryPocketCoverage("avatar-shirt", id)?.Covered == true), "zipped coat covers both shirt mouths");
        Check(!sim.QuoteInventory(new(InventoryActionKind.Move, "spare-dressing", new("person:avatar", "hand"))).Allowed, "covered pocket contents refuse removal");
        var covered = sim.Save(); clone.Restore(covered); Check(clone.Save() == covered, "covered badge can remain attached across save/restore");
        Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Check(sim.InventoryMoveAccess("personnel-badge").Allowed && pockets.All(id => sim.InventoryPocketCoverage("avatar-shirt", id)?.Covered == false), "unzip restores contact and pocket access");
        Act(new(InventoryActionKind.Move, "personnel-badge", new("person:avatar", "hand")));
        Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(!sim.QuoteInventory(attach).Allowed, "badge cannot be attached through covered clip");
        Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Act(attach); Act(sim.GroundDropRequest("avatar-backpack")); Act(sim.GroundDropRequest("avatar-coat")); Act(sim.GroundDropRequest("avatar-shirt"));
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "personnel-badge").Placement.ParentId == "avatar-shirt", "removing loaded shirt keeps badge attached");
        Console.WriteLine($"SHIRT_BADGE_CHECKS_PASS: {count} assertions; two pockets, coverage, physical clip, covered retention/access, costs and continuation.");
        return count;
    }
}
