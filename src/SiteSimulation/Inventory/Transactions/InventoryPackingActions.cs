namespace SiteSimulation;

/// <summary>Access, costs and atomic plan validation for container-target insertion and local arrangement.</summary>
internal static class InventoryPackingActions
{
    public static InventoryPackingPlan Quote(SimulationWorld world, string actorId, InventoryRequest request, out int duration, out int ap)
    {
        duration = ap = 0;
        if (request.Destination is not { X: 0, Y: 0, Rotated: false } target || request.RegionId is not null)
            throw new ArgumentException("Choose a compartment for packing.");
        var state = world.Inventory;
        if (request.Kind == InventoryActionKind.Arrange && request.ItemId != target.ParentId)
            throw new ArgumentException("Arrange addresses one compartment of its owner.");
        var access = InventoryAccessService.Placement(world, actorId, target);
        if (!access.Allowed) throw new ArgumentException(access.Reason);
        var incoming = request.Kind == InventoryActionKind.AutoPlace ? request.ItemId : null;
        if (incoming is not null)
        {
            if (!state.Items.TryGetValue(incoming, out var item)) throw new ArgumentException("Unknown incoming item.");
            var source = InventoryAccessService.Placement(world, actorId, item.Placement);
            if (!source.Allowed) throw new ArgumentException(source.Reason);
            InventoryWear.ValidateAction(state, item, item.Placement);
            if (item.Placement.ParentId == target.ParentId && item.Placement.RegionId == target.RegionId)
                throw new ArgumentException("Item is already in this compartment; place it on the grid or choose Arrange explicitly.");
            if (item.Id == target.ParentId) throw new ArgumentException("A container cannot be placed inside itself.");
            for (var parent = target.ParentId; state.Items.TryGetValue(parent, out var ancestor); parent = ancestor.Placement.ParentId)
                if (ancestor.Id == item.Id) throw new ArgumentException("A container cannot be placed inside its descendant.");
        }
        var plan = InventoryPacking.Plan(state, target.ParentId, target.RegionId, incoming);
        foreach (var move in plan.Moves)
        {
            var definition = state.Definition(move.ItemId);
            duration += definition.HandleMs + InventoryAccessService.HandlingMs(state, move.From) + InventoryAccessService.HandlingMs(state, move.To);
            ap += definition.HandleAp + InventoryActions.AccessAp(state, move.From) + InventoryActions.AccessAp(state, move.To);
        }
        if (duration > 100000) throw new ArgumentException("Too much packing for one supported handling action; use smaller compartments.");
        return plan;
    }
    internal static bool Same(InventoryPackingPlan? first, InventoryPackingPlan? second)
        => first is null ? second is null : second is not null && first.Moves is not null && second.Moves is not null && first.Repacked == second.Repacked && first.Moves.SequenceEqual(second.Moves);
}
