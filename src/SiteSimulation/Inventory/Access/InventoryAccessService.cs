namespace SiteSimulation;

internal static class InventoryAccessService
{
    public static InventoryAccess Root(SimulationWorld world, string actorId, InventoryRoot root)
    {
        if (root.ActorId is not null) return new(root.ActorId == actorId, root.ActorId == actorId ? "Accessible" : "Another person's equipment; no direct access");
        var actor = world.Actors.Single(a => a.Id == actorId);
        var prop = world.Map.Definition.Props.Single(p => p.Id == root.PropId);
        var allowed = Point2.Distance(actor.Position, prop.Cell.World) <= 1.35 && world.Map.ClearSight(actor.Position, prop.Cell.World);
        return new(allowed, allowed ? "Within reach" : "Approach the fixture with clear access");
    }
    public static InventoryAccess Placement(SimulationWorld world, string actorId, ItemPlacement placement, bool equipment = false)
    {
        var state = world.Inventory;
        var physicalRoot = InventoryHierarchy.Root(state, placement.ParentId);
        if (equipment && physicalRoot.ActorId != actorId) return new(false, "Equipment is not carried by this person");
        var rootAccess = Root(world, actorId, physicalRoot);
        if (!rootAccess.Allowed) return rootAccess;
        var id = placement.ParentId;
        var regionId = placement.RegionId;
        for (var depth = 0; depth <= InventoryHierarchy.MaxDepth; depth++)
        {
            var region = state.Region(id, regionId);
            if (!state.IsOpen(id, regionId)) return new(false, region.Name + " is closed");
            if (equipment && !region.EquipmentReady) return new(false, region.Name + " is packed storage; move equipment to an accessible pocket or hand");
            var root = state.Content!.Roots.SingleOrDefault(r => r.Id == id);
            if (root is not null)
            {
                if (equipment && root.ActorId != actorId) return new(false, "Equipment is not carried by this person");
                return Root(world, actorId, root);
            }
            var parent = state.Items[id];
            id = parent.Placement.ParentId; regionId = parent.Placement.RegionId;
        }
        return new(false, "Inventory hierarchy is too deep");
    }
    public static bool Capability(SimulationWorld world, string actorId, string capability) => world.Inventory.Enabled
        && world.Inventory.Items.Values.Any(i => world.Inventory.Definition(i.Id).Capability == capability && i.Powered && i.Condition > 0
            && Placement(world, actorId, i.Placement, true).Allowed);
    public static int HandlingMs(InventoryState state, ItemPlacement placement)
    {
        var total = 0;
        while (true)
        {
            total += state.Region(placement.ParentId, placement.RegionId).AccessMs;
            if (!state.Items.TryGetValue(placement.ParentId, out var parent)) return total;
            placement = parent.Placement;
        }
    }
}
