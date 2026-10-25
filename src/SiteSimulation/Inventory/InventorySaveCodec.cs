namespace SiteSimulation;

internal static class InventorySaveCodec
{
    internal static void ValidateGraph(InventoryState state)
    {
        var content = state.Content!;
        if (!state.Items.Keys.Order().SequenceEqual(content.Items.Select(i => i.Id).Order())
            || state.Items.Values.Any(i => i.Placement is null || i.Condition is < 0 or > 100
                || !content.Items.Any(original => original.Id == i.Id && original.DefinitionId == i.DefinitionId)
                || !state.Items.ContainsKey(i.Placement.ParentId) && !content.Roots.Any(r => r.Id == i.Placement.ParentId))
            || state.Regions.Any(r => r is null || !state.Items.ContainsKey(r.ItemId)
                || state.Definition(r.ItemId).Regions?.Any(region => region.Id == r.RegionId) != true)
            || state.Regions.Select(r => (r.ItemId, r.RegionId)).Distinct().Count() != state.Regions.Count
            || state.Regions.Count != content.Regions.Length)
            throw new ArgumentException("Invalid inventory identity, membership or compartment state.");
        try { InventoryPlacement.Validate(state); }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { throw new ArgumentException("Invalid inventory graph.", ex); }
    }
    internal static void Restore(SimulationWorld world, InventorySnapshot? saved)
    {
        var state = world.Inventory;
        if (!state.Enabled)
        {
            if (saved is not null) throw new ArgumentException("Inventory state has no matching scenario.");
            return;
        }
        if (saved is null || saved.Items is null || saved.Regions is null || saved.Items.Any(i => i is null)
            || saved.Items.Select(i => i.Id).Distinct().Count() != saved.Items.Length || saved.Revision < 0)
            throw new ArgumentException("Invalid inventory snapshot.");
        state.Items.Clear(); foreach (var item in saved.Items) state.Items.Add(item.Id, item);
        state.Regions.Clear(); state.Regions.AddRange(saved.Regions); state.Revision = saved.Revision;
        ValidateGraph(state);
        if (saved.Pending is { } work)
        {
            if (work.ActorId != world.Player.Id || work.Request is null || work.Revision != saved.Revision
                || work.RemainingMs <= 0 || work.RemainingMs > work.TotalMs || work.RemainingMs % SpatialSimulation.TickMs != 0
                || work.TotalMs <= 0 || work.TotalMs > 100000 || work.TotalMs % SpatialSimulation.TickMs != 0
                || work.CostAp < 1 || work.AbortReason is { Length: > 250 } || work.Position != world.Player.Position || world.Player.Moving)
                throw new ArgumentException("Invalid inventory handling continuation.");
            // An interrupted action can remain paid while resolving; its original physical request must still be structurally valid.
            if (!InventoryActions.Validate(world, work.ActorId, work.Request, out _, out var duration, out var ap)
                || duration != work.TotalMs || ap != work.CostAp)
                throw new ArgumentException("Invalid inventory request or declared cost.");
            var tactical = world.Combat.Pending;
            if (work.Tactical != world.Combat.Active || work.Tactical != (tactical?.Kind == CombatActionKind.Inventory)
                || work.Tactical && (tactical!.ActorId != work.ActorId || tactical.RemainingMs != work.RemainingMs
                    || tactical.TotalMs != work.TotalMs || tactical.Cost != work.CostAp))
                throw new ArgumentException("Inventory and tactical schedules disagree.");
        }
        else if (world.Combat.Pending?.Kind == CombatActionKind.Inventory) throw new ArgumentException("Missing paid inventory continuation.");
        state.Pending = saved.Pending;
    }
}
