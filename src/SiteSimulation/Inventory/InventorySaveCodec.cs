namespace SiteSimulation;

internal static class InventorySaveCodec
{
    internal static void ValidateGraph(InventoryState state)
    {
        var content = state.Content!;
        if (!state.Items.Keys.Order().SequenceEqual(content.Items.Select(i => i.Id).Order())
            || state.Items.Values.Any(i => i.Placement is null || i.Condition is < 0 or > 100
                || !content.Items.Any(original => original.Id == i.Id && original.DefinitionId == i.DefinitionId)
                || i.GarmentOpen && state.Definition(i.Id).Wear?.OpenExposes is not { Length: > 0 }
                || i.SecureOnClose is not null && state.Definition(i.Id).Wear?.OpenExposes is not { Length: > 0 }
                || !state.Items.ContainsKey(i.Placement.ParentId) && !content.Roots.Any(r => r.Id == i.Placement.ParentId))
            || state.Regions.Any(r => r is null || !state.Items.ContainsKey(r.ItemId)
                || state.Definition(r.ItemId).Regions?.Any(region => region.Id == r.RegionId && region.Attachment is null) != true)
            || state.Regions.Any(r => !state.Region(r.ItemId, r.RegionId).Sealable && (!r.Open || r.SecureOnClose is not null))
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
            if (!InventoryActions.Validate(world, work.ActorId, work.Request, out _, out var duration, out var ap, out var packing)
                || duration != work.TotalMs || ap != work.CostAp || !InventoryPackingActions.Same(work.Packing, packing))
                throw new ArgumentException("Invalid inventory request or declared cost.");
            if (InventoryWornOrder.Resolve(state, work.Request) != work.Request)
                throw new ArgumentException("Saved handling is missing its resolved dressing order.");
            var tactical = world.Combat.Pending;
            if (work.Tactical != world.Combat.Active || work.Tactical != (tactical?.Kind == CombatActionKind.Inventory)
                || work.Tactical && (tactical!.ActorId != work.ActorId || tactical.RemainingMs != work.RemainingMs
                    || tactical.TotalMs != work.TotalMs || tactical.Cost != work.CostAp))
                throw new ArgumentException("Inventory and tactical schedules disagree.");
        }
        else if (world.Combat.Pending?.Kind == CombatActionKind.Inventory) throw new ArgumentException("Missing paid inventory continuation.");
        state.Pending = saved.Pending;
        var queue = saved.SecureQueue ?? [];
        if (queue.Length > InventorySecuring.MaxSteps || queue.Any(s => s is null || s.Request is null || s.Placement is null
            || !InventorySecuring.IsSecure(s.Request) || s.Request.Destination is not null
            || !state.Items.TryGetValue(s.Request.ItemId, out var item) || item.Placement != s.Placement
            || (s.Request.Kind == InventoryActionKind.Close) != (s.Request.RegionId is not null))
            || queue.Select(s => s.Request).Distinct().Count() != queue.Length)
            throw new ArgumentException("Invalid inventory securing continuation.");
        foreach (var step in queue)
            if (!InventoryActions.Validate(world, world.Player.Id, step.Request, out var reason, out _, out _)
                && reason is not ("Compartment is already in that state" or "Garment is already in that state"))
                throw new ArgumentException("Invalid inventory securing target: " + reason);
        state.SecureQueue.Clear(); state.SecureQueue.AddRange(queue);
    }
}
