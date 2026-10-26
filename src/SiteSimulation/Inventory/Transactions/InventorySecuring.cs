namespace SiteSimulation;

/// <summary>Explicit close intent, persisted independently of UI lifetime. Each step buys normal handling.</summary>
internal static class InventorySecuring
{
    internal const int MaxSteps = 256;
    internal static bool IsSecure(InventoryRequest r) => r.Kind is InventoryActionKind.Close or InventoryActionKind.CloseGarment;
    internal static bool Completed(InventoryState state, InventoryRequest r) => r.Kind == InventoryActionKind.CloseGarment
        ? !state.Items[r.ItemId].GarmentOpen : !state.IsOpen(r.ItemId, r.RegionId!);

    public static bool Prepare(SimulationWorld world, IEnumerable<InventoryRequest> requests, out string reason, bool automatic = false)
    {
        reason = "Finish the current handling or securing sequence first.";
        var state = world.Inventory;
        if (!state.Enabled || state.Pending is not null || state.SecureQueue.Count != 0) return false;
        var entries = requests.Take(MaxSteps + 1).ToArray();
        if (entries.Length > MaxSteps || entries.Any(r => r is null || !IsSecure(r) || r.Destination is not null
            || !state.Items.ContainsKey(r.ItemId) || (r.Kind == InventoryActionKind.Close) != (r.RegionId is not null)))
        { reason = "Invalid securing targets."; return false; }
        var steps = new List<InventorySecureStep>();
        foreach (var request in entries.Distinct())
        {
            // Validate even completed requests structurally; only the already-state rejection is harmless.
            var valid = InventoryActions.Validate(world, world.Player.Id, request, out reason, out _, out var ap);
            if (!valid)
            {
                if (reason is "Compartment is already in that state" or "Garment is already in that state") continue;
                return false;
            }
            if (world.Combat.Active && ap > CombatState.TurnAp)
            { reason = $"This closure needs {ap} AP, more than one supported turn. Close deeper containers separately."; return false; }
            steps.Add(new(request, state.Items[request.ItemId].Placement, automatic));
        }
        // Contained closures first; open inner garments before outer garments can cover their fastenings.
        state.SecureQueue.AddRange(steps.OrderByDescending(s => Depth(state, s.Request.ItemId))
            .ThenBy(s => s.Request.Kind == InventoryActionKind.CloseGarment ? 1 : 0)
            .ThenBy(s => state.Definition(s.Request.ItemId).Wear?.Layer ?? 0));
        reason = steps.Count == 0 ? "These containers are already secured." : $"Securing queued: {steps.Count} closures; normal time/AP apply. Cancel handling to stop.";
        if (steps.Count != 0) PlayerFeedback.Add(world, MessageKind.Equipment, "Inventory: " + reason);
        return true;
    }
    private static int Depth(InventoryState state, string id)
    {
        var depth = 0;
        while (state.Items.TryGetValue(id, out var item)) { depth++; id = item.Placement.ParentId; }
        return depth;
    }
    public static void TryStart(SimulationWorld world)
    {
        var state = world.Inventory;
        if (state.Pending is not null || state.SecureQueue.Count == 0 || world.LocalMs < world.Player.StartledUntilMs) return;
        if (world.Combat.Active && !TacticalCombatSystem.CanAct(world, world.Player.Id, out _)) return;
        var step = state.SecureQueue[0];
        if (step.Automatic && !InventoryClosures.SecureOnClose(state, step.Request.ItemId, step.Request.RegionId))
        { state.SecureQueue.RemoveAt(0); return; }
        if (state.Items[step.Request.ItemId].Placement != step.Placement)
        { Stop(world, "An inspected object moved; inspect it again before securing."); return; }
        if (Completed(state, step.Request)) { state.SecureQueue.RemoveAt(0); return; }
        if (!InventoryActions.Validate(world, world.Player.Id, step.Request, out var reason, out _, out var ap))
        { Stop(world, reason); return; }
        // Wait for a later player turn without ending the current turn or spending partial AP.
        if (world.Combat.Active && world.Combat.For(world.Player.Id)!.Ap < ap) return;
        if (!InventoryActions.Start(world, step.Request, out reason)) { Stop(world, reason); return; }
        state.SecureQueue.RemoveAt(0);
    }
    private static void Stop(SimulationWorld world, string reason)
    {
        world.Inventory.SecureQueue.Clear();
        PlayerFeedback.Add(world, MessageKind.Warning, "Inventory: securing stopped: " + reason);
    }
}
