namespace SiteSimulation;

/// <summary>Timed handling, final revalidation and atomic item moves. No UI packing rules.</summary>
internal static class InventoryActions
{
    public static bool Validate(SimulationWorld world, string actorId, InventoryRequest request, out string reason, out int duration, out int ap)
        => Validate(world, actorId, request, out reason, out duration, out ap, out _);
    internal static bool Validate(SimulationWorld world, string actorId, InventoryRequest request, out string reason, out int duration, out int ap,
        out InventoryPackingPlan? packing)
    {
        var state = world.Inventory; duration = 0; ap = 0; packing = null;
        reason = "Inventory is unavailable";
        if (state.Enabled && request?.Kind is InventoryActionKind.AutoPlace or InventoryActionKind.Arrange)
        {
            try { packing = InventoryPackingActions.Quote(world, actorId, request, out duration, out ap); reason = "Ready"; return true; }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { reason = ex is ArgumentException ? ex.Message : "Unknown packing destination."; return false; }
        }
        if (!state.Enabled || request is null || !Enum.IsDefined(request.Kind) || !state.Items.TryGetValue(request.ItemId, out var item)) return false;
        var access = InventoryAccessService.Placement(world, actorId, item.Placement);
        if (!access.Allowed) { reason = access.Reason; return false; }
        var definition = state.Definition(item.Id);
        duration = definition.HandleMs + InventoryAccessService.HandlingMs(state, item.Placement);
        ap = definition.HandleAp + AccessAp(state, item.Placement);
        try
        {
            switch (request.Kind)
            {
                case InventoryActionKind.Move:
                    if (request.Destination is not { } destination || request.RegionId is not null) throw new ArgumentException("Choose a physical destination");
                    if (destination == item.Placement) throw new ArgumentException("Item is already there");
                    InventoryWear.ValidateAction(state, item, item.Placement);
                    var target = InventoryAccessService.Placement(world, actorId, destination);
                    if (!target.Allowed) throw new ArgumentException(target.Reason);
                    InventoryWear.ValidateAction(state, item, destination);
                    duration += InventoryAccessService.HandlingMs(state, destination);
                    ap += AccessAp(state, destination);
                    var candidate = new InventoryState(state);
                    candidate.Items[item.Id] = item with { Placement = destination };
                    InventoryPlacement.Validate(candidate);
                    break;
                case InventoryActionKind.Open:
                case InventoryActionKind.Close:
                    if (request.RegionId is null || request.Destination is not null) throw new ArgumentException("Choose a compartment");
                    var region = state.Region(item.Id, request.RegionId);
                    var compartmentAccess = InventoryWear.Compartment(state, item.Id, region);
                    if (!compartmentAccess.Allowed) throw new ArgumentException(compartmentAccess.Reason);
                    if (state.IsOpen(item.Id, region.Id) == (request.Kind == InventoryActionKind.Open)) throw new ArgumentException("Compartment is already in that state");
                    duration = Math.Max(150, region.AccessMs + InventoryAccessService.HandlingMs(state, item.Placement));
                    ap = Math.Max(1, region.AccessAp + AccessAp(state, item.Placement));
                    break;
                case InventoryActionKind.OpenGarment:
                case InventoryActionKind.CloseGarment:
                    if (definition.Wear is not { OpenExposes.Length: > 0 } wear || request.RegionId is not null || request.Destination is not null)
                        throw new ArgumentException("This garment has no supported opening");
                    if (item.GarmentOpen == (request.Kind == InventoryActionKind.OpenGarment)) throw new ArgumentException("Garment is already in that state");
                    InventoryWear.ValidateAction(state, item, item.Placement, wear.OpenExposes, fastening: true);
                    duration = wear.FastenMs + InventoryAccessService.HandlingMs(state, item.Placement);
                    ap = wear.FastenAp + AccessAp(state, item.Placement);
                    break;
                case InventoryActionKind.PowerOn:
                case InventoryActionKind.PowerOff:
                    if (definition.Capability is null || request.Destination is not null || request.RegionId is not null) throw new ArgumentException("This item has no power control");
                    if (item.Powered == (request.Kind == InventoryActionKind.PowerOn)) throw new ArgumentException("Equipment is already in that power state");
                    break;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or NullReferenceException)
        { reason = ex is ArgumentException ? ex.Message : "Invalid physical destination or compartment"; return false; }
        reason = "Ready"; return true;
    }
    internal static int AccessAp(InventoryState state, ItemPlacement placement)
    {
        var cost = 0;
        for (var depth = 0; depth <= InventoryHierarchy.MaxDepth; depth++)
        {
            cost += state.Region(placement.ParentId, placement.RegionId).AccessAp;
            if (!state.Items.TryGetValue(placement.ParentId, out var parent)) return cost;
            placement = parent.Placement;
        }
        throw new ArgumentException("Inventory hierarchy is too deep");
    }
    public static bool Start(SimulationWorld world, InventoryRequest request, out string reason)
    {
        var actor = world.Player;
        reason = world.ManualPause || world.LocalHold ? "Inventory handling waits with the local simulation."
            : world.Inventory.Pending is not null ? "Finish the current inventory action first."
            : world.LocalMs < actor.StartledUntilMs ? "Recovering from startle." : "Ready";
        if (reason != "Ready") return false;
        if (!Validate(world, actor.Id, request, out reason, out var duration, out var ap, out var packing)) return false;
        if (world.Combat.Active && !TacticalCombatSystem.HandleInventory(world, ap, duration, out reason)) return false;
        MovementSystem.StopInPlace(actor);
        world.Inventory.Pending = new(actor.Id, request, actor.Position, duration, duration, world.Inventory.Revision, ap, world.Combat.Active, Packing: packing);
        reason = $"Handling {world.Inventory.Name(request.ItemId)}: {duration / 1000.0:0.00}s" + (world.Combat.Active ? $" · {ap} AP" : "");
        PlayerFeedback.Add(world, MessageKind.Equipment, "Inventory: " + reason);
        return true;
    }
    public static void Cancel(SimulationWorld world, string reason)
    {
        world.Inventory.SecureQueue.Clear();
        if (world.Inventory.Pending is not { } pending) return;
        if (pending.Tactical) { world.Inventory.Pending = pending with { AbortReason = reason }; return; }
        world.Inventory.Pending = null;
        PlayerFeedback.Add(world, MessageKind.Warning, "Inventory: action interrupted: " + reason);
    }
    public static void Advance(SimulationWorld world)
    {
        var state = world.Inventory;
        if (state.Pending is not { } work) return;
        var actor = world.Actors.Single(a => a.Id == work.ActorId);
        var abort = work.AbortReason ?? (actor.Position != work.Position ? "Character moved" : world.LocalMs < actor.StartledUntilMs ? "Startled while handling equipment" : null);
        work = work with { RemainingMs = work.RemainingMs - SpatialSimulation.TickMs, AbortReason = abort };
        state.Pending = work;
        if (work.RemainingMs > 0) return;
        state.Pending = null;
        if (work.AbortReason is not null || work.Revision != state.Revision
            || !Validate(world, work.ActorId, work.Request, out _, out _, out _, out var verified)
            || !InventoryPackingActions.Same(work.Packing, verified))
        { state.SecureQueue.Clear(); PlayerFeedback.Add(world, MessageKind.Warning, "Inventory: action failed: " + (work.AbortReason ?? "Item, access or destination changed")); return; }
        if (work.Packing is { } plan)
        {
            foreach (var move in plan.Moves) state.Items[move.ItemId] = state.Items[move.ItemId] with { Placement = move.To };
            state.Revision++;
            world.Log("inventory", work.Request.ItemId, work.Request.Kind.ToString(), "Atomic region packing committed.");
            PlayerFeedback.Add(world, MessageKind.Equipment, "Inventory: " + state.Name(work.Request.ItemId) + ": " + work.Request.Kind + " completed.");
            return;
        }
        var request = work.Request; var item = state.Items[request.ItemId];
        switch (request.Kind)
        {
            case InventoryActionKind.Move: state.Items[item.Id] = item with { Placement = request.Destination! }; break;
            case InventoryActionKind.Open:
            case InventoryActionKind.Close:
                var index = state.Regions.FindIndex(r => r.ItemId == item.Id && r.RegionId == request.RegionId);
                state.Regions[index] = state.Regions[index] with { Open = request.Kind == InventoryActionKind.Open }; break;
            case InventoryActionKind.PowerOn:
            case InventoryActionKind.PowerOff: state.Items[item.Id] = item with { Powered = request.Kind == InventoryActionKind.PowerOn }; break;
            case InventoryActionKind.OpenGarment:
            case InventoryActionKind.CloseGarment: state.Items[item.Id] = item with { GarmentOpen = request.Kind == InventoryActionKind.OpenGarment }; break;
        }
        state.Revision++;
        world.Log("inventory", item.Id, request.Kind.ToString(), "Timed handling committed; one physical item graph.");
        PlayerFeedback.Add(world, MessageKind.Equipment, "Inventory: " + state.Definition(item.Id).Name + ": " + request.Kind + " completed.");
    }
}
