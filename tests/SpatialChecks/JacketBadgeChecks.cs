using SiteSimulation;

internal static class JacketBadgeChecks
{
    public static int Run(string map, string config)
    {
        var count = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Jacket badge: " + reason); count++; }
        var sim = new SpatialSimulation(map, config); sim.Player.Cell = new(13, 6);
        void Finish() { for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) sim.Tick(); Check(sim.Inventory.Pending is null, "handling finishes"); }
        void Act(InventoryRequest request) { Check(sim.HandleInventory(request), sim.LastMessage); Finish(); }
        InventoryRequest Mount(string owner) => new(InventoryActionKind.Move, "personnel-badge", new(owner, "badge-clip", AttachmentPose: new(0, 2)));
        var definition = sim.Inventory.Definition("avatar-coat");
        Check(definition.Regions!.Count(r => r.Attachment is null) == 4 && definition.Regions!.Count(r => r.Attachment is not null) == 1,
            "four real pockets and a separate exterior clip");
        Check(definition.Regions!.Single(r => r.Id == "badge-clip").Attachment is { RequiresExposedAccess: true, Interface: "badge-clip", Surface: "front" }, "clip shares physical badge interface");
        Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        sim.ManualPause = true; var paused = sim.Save();
        Check(!sim.HandleInventory(Mount("avatar-coat")) && sim.Save() == paused, "paused mounting refuses"); sim.ManualPause = false;
        Check(sim.HandleInventory(Mount("avatar-coat")), sim.LastMessage);
        Check(sim.Inventory.Pending is { TotalMs: > 0, CostAp: > 0 } && sim.Inventory.Items["personnel-badge"].Placement.ParentId == "spare-backpack", "paid mount retains original badge until commitment");
        sim.Tick(); var pending = sim.Save(); var clone = new SpatialSimulation(map, config); clone.Restore(pending); Check(clone.Save() == pending, "pending mount continuation is exact"); Finish();
        Check(sim.Inventory.Items["personnel-badge"].Placement is { ParentId: "avatar-coat", RegionId: "badge-clip" }, "one badge attached to jacket");
        sim.Player.Cell = new(15, 2);
        Check(sim.ToggleNearestDoor() && sim.Inventory.Pending is null && sim.Map.OpenDoors.Contains("door-7-5-u"), "closed jacket badge authorizes ordinary reader use");
        Check(sim.ToggleNearestDoor(), "close reader for cover test");
        Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Check(sim.InventoryMoveAccess("personnel-badge").Allowed, "opening jacket retains exposed badge");
        Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(!sim.QuoteInventory(Mount("avatar-shirt")).Allowed, "cannot attach through jacket to shirt underneath");
        Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "front")));
        Check(!sim.InventoryMoveAccess("personnel-badge").Allowed && !sim.ToggleNearestDoor(), "front pack covers real badge and denies access/entry");
        var covered = sim.Save(); clone.Restore(covered); Check(clone.Save() == covered, "covered jacket badge persists without detaching");
        Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "back")));
        Check(sim.InventoryMoveAccess("personnel-badge").Allowed, "uncover restores access");
        Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Act(Mount("avatar-shirt")); Check(sim.Inventory.Items["personnel-badge"].Placement.ParentId == "avatar-shirt", "transfer to shirt keeps same physical badge");
        Act(Mount("avatar-coat"));
        Act(sim.GroundDropRequest("avatar-backpack"));
        var badgeCount = sim.Inventory.Items.Values.Count(i => i.DefinitionId == "personnel-badge");
        Check(sim.HandleInventory(sim.GroundDropRequest("avatar-coat")), sim.LastMessage);
        Check(sim.Inventory.Items["personnel-badge"].Placement.ParentId == "avatar-coat", "loaded removal retains child during handling"); Finish();
        Check(sim.Inventory.Items["personnel-badge"].Placement.ParentId == "avatar-coat" && sim.Inventory.Items.Values.Count(i => i.DefinitionId == "personnel-badge") == badgeCount, "loaded removal never clones or loses badge");
        Act(new(InventoryActionKind.Move, "avatar-coat", new("person:avatar", "hand")));
        Check(!sim.ToggleNearestDoor(), "holding a jacket with attached badge does not equip the badge in hand or wear it");
        Act(sim.GroundDropRequest("avatar-coat"));
        Act(new(InventoryActionKind.Move, "personnel-badge", new("person:avatar", "hand")));
        Check(sim.ToggleNearestDoor() && sim.Inventory.Pending?.Request.Kind == InventoryActionKind.PresentBadge, "removed badge directly in hand buys manual presentation"); Finish();
        Check(sim.Map.OpenDoors.Contains("door-7-5-u"), "manual held badge remains functional");
        Console.WriteLine($"JACKET_BADGE_CHECKS_PASS: {count} assertions; separate clip, paid attachment/transfer, cover, reader use, loaded removal and continuation.");
        return count;
    }
}
