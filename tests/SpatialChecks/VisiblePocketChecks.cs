using SiteSimulation;

internal static class VisiblePocketChecks
{
    public static int Run(string map, string config)
    {
        var count = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Visible pockets: " + reason); count++; }
        var sim = new SpatialSimulation(map, config); sim.Player.Cell = new(13, 6);
        void Act(InventoryRequest request)
        {
            Check(sim.HandleInventory(request), sim.LastMessage);
            for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) sim.Tick();
            Check(sim.Inventory.Pending is null, "handling finishes");
        }
        foreach (var (owner, ids) in new[] {
            ("avatar-coat", new[] { "chest", "chest-left", "lower-left", "lower-right" }),
            ("avatar-trousers", new[] { "left", "right", "cargo-left", "cargo-right", "rear-left", "rear-right" }),
            ("avatar-backpack", new[] { "main", "side", "front" }) })
            Check(sim.Inventory.Definition(owner).Regions!.Where(r => r.Attachment is null).Select(r => r.Id).SequenceEqual(ids), "complete depicted pocket set: " + owner);
        Act(new(InventoryActionKind.Move, "spare-dressing", new("person:avatar", "hand")));
        Act(sim.GroundDropRequest("avatar-backpack"));
        sim.Player.Cell = new(12, 6);
        foreach (var rear in new[] { "rear-left", "rear-right" })
            Check(sim.InventoryPocketCoverage("avatar-trousers", rear)!.Covered, "open coat still covers rear mouths");
        Act(sim.GroundDropRequest("avatar-coat"));
        foreach (var rear in new[] { "rear-left", "rear-right" })
            Check(!sim.InventoryPocketCoverage("avatar-trousers", rear)!.Covered, "removing coat exposes rear mouths");
        sim.Player.Cell = new(13, 6);
        foreach (var (owner, ids) in new[] {
            ("avatar-coat", new[] { "chest-left", "lower-left", "lower-right" }),
            ("avatar-trousers", new[] { "rear-left", "rear-right" }),
            ("avatar-backpack", new[] { "front" }) })
        foreach (var id in ids)
        {
            sim.Player.Cell = owner == "avatar-coat" ? new(12, 6) : new(13, 6);
            Check(!sim.Inventory.IsOpen(owner, id), "new pocket initially secured: " + id);
            Check(!sim.QuoteInventoryPacking(new(InventoryActionKind.AutoPlace, "spare-dressing", new(owner, id))).Allowed, "closed pocket refuses storage");
            Act(new(InventoryActionKind.Open, owner, RegionId: id));
            Act(new(InventoryActionKind.AutoPlace, "spare-dressing", new(owner, id)));
            Check(sim.Inventory.Items["spare-dressing"].Placement is var p && p.ParentId == owner && p.RegionId == id, "storage uses exact compartment");
            var saved = sim.Save(); var copy = new SpatialSimulation(map, config); copy.Restore(saved);
            Check(copy.Save() == saved, "loaded pocket and closure continuation is exact");
            Act(new(InventoryActionKind.Move, "spare-dressing", new("person:avatar", "hand")));
            Act(new(InventoryActionKind.Close, owner, RegionId: id));
        }
        foreach (var id in new[] { "rear-left", "rear-right" })
            Check(sim.Inventory.Region("avatar-trousers", id).AccessFootprint!.All(m => m.Surface == "back"), "rear mouth is on back");
        Console.WriteLine($"VISIBLE_POCKET_CHECKS_PASS: {count} assertions; complete region sets, independent closures, paid storage, rear mouths and continuation.");
        return count;
    }
}
