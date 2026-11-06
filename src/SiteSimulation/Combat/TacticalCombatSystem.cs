namespace SiteSimulation;

/// <summary>Local scheduler and AP transactions. Training equipment/points deliberately remain bounded fixtures.</summary>
internal static class TacticalCombatSystem
{
    public static bool Arm(SimulationWorld world, string opponentId, out string reason)
    {
        reason = "Finish the current drill or release the local hold first.";
        if (world.Combat.Armed || !world.LocalActive) return false;
        if (!world.Actors.Any(a => a.Id == opponentId && a != world.Player)) { reason = "Choose a staff training opponent."; return false; }
        var state = world.Combat;
        state.Roster.Clear(); state.Contacts.Clear();
        foreach (var actor in world.Actors.OrderBy(a => a.Id, StringComparer.Ordinal))
            state.Roster.Add(new(actor.Id, actor.Id == opponentId ? "opposition" : "friendly", 1 + (int)(state.Roll() % 20)));
        state.Roster.Sort((a, b) => a.Initiative != b.Initiative ? b.Initiative.CompareTo(a.Initiative) : string.CompareOrdinal(a.ActorId, b.ActorId));
        state.Armed = true; state.Round = 0; state.Cursor = 0;
        reason = "Training armed: contact with the selected opponent starts local turns. No live weapons or injuries.";
        PlayerFeedback.Add(world, MessageKind.Status, reason);
        return true;
    }
    public static void Detect(SimulationWorld world)
    {
        var state = world.Combat;
        if (!state.Armed || state.Active || !world.LocalActive) return;
        if (!state.Roster.Any(c => c.Side == "opposition" && world.Player.Cell.Distance(Actor(world, c.ActorId).Cell) <= CombatState.EngagementHexes
            && PlayerFeedback.Sees(world, Actor(world, c.ActorId)))) return;
        // The facade evaluates due anomaly effects before reaching this scheduler. Entry is not a proximity shield.
        InventoryActions.Cancel(world, "Combat began; reissue handling as a paid action");
        foreach (var actor in world.Actors) MovementSystem.StopInPlace(actor);
        ObservedTransportSystem.Interrupt(world);
        state.Active = true; state.Round = 1;
        BeginTurn(world);
        PlayerFeedback.Add(world, MessageKind.Combat, "Training contact: local turns begin. The world calendar continues while you choose.");
    }
    private static ActorState Actor(SimulationWorld world, string id) => world.Actors.Single(a => a.Id == id);
    private static void Replace(CombatState state, Combatant value) => state.Roster[state.Roster.FindIndex(c => c.ActorId == value.ActorId)] = value;
    private static void BeginTurn(SimulationWorld world)
    {
        var state = world.Combat;
        Replace(state, state.Roster[state.Cursor] with { Ap = CombatState.TurnAp });
        if (state.CurrentActorId == world.Player.Id) PlayerFeedback.Add(world, MessageKind.Combat, $"Your turn · round {state.Round} · {CombatState.TurnAp} AP.");
    }
    public static bool CanAct(SimulationWorld world, string actorId, out string reason)
    {
        reason = world.ManualPause || world.LocalHold ? "Release the pause/hold first."
            : !world.Combat.Active || world.Combat.CurrentActorId != actorId ? "Wait for your turn."
            : world.Combat.Pending is not null ? "An action is still resolving."
            : world.LocalMs < Actor(world, actorId).StartledUntilMs ? "Recovering from startle: end this turn to wait." : "accepted";
        return reason == "accepted";
    }
    private static bool Commit(SimulationWorld world, string actorId, CombatActionKind kind, int cost, int duration,
        out string reason, string? targetId = null, Point2? point = null, int steps = 0)
    {
        if (!CanAct(world, actorId, out reason)) return false;
        var state = world.Combat; var participant = state.For(actorId)!;
        if (participant.Ap < cost) { reason = $"Needs {cost} AP; {participant.Ap} remain."; return false; }
        Replace(state, participant with { Ap = participant.Ap - cost });
        state.Pending = new(state.NextActionId++, actorId, kind, cost, duration, duration, targetId, point, steps);
        world.Log("combat", actorId, "action-committed", $"{kind}; {cost} AP; {duration}ms local");
        reason = $"{kind} · {cost} AP";
        return true;
    }
    public static bool HandleInventory(SimulationWorld world, int cost, int duration, out string reason)
        => Commit(world, world.Player.Id, CombatActionKind.Inventory, cost, duration, out reason);
    public static bool Move(SimulationWorld world, string actorId, Hex destination, bool keepFacing, out string reason)
    {
        if (!CanAct(world, actorId, out reason)) return false;
        var actor = Actor(world, actorId);
        var path = actor == world.Player ? PlayerNavigation.PlanRoute(world, destination) : MovementSystem.PlanRoute(world, actor, destination);
        if (path.Count == 0) { reason = "No closer reachable position."; return false; }
        var duration = path.Count * SpatialSimulation.StepDurationMs;
        if (actor.StoppedStep is { } stopped) duration -= path[0] == stopped.Next ? stopped.ProgressMs : SpatialSimulation.StepDurationMs - stopped.ProgressMs;
        if (!Commit(world, actorId, CombatActionKind.Move, path.Count, duration, out reason, point: destination.World, steps: path.Count)) return false;
        // No unpaid continuation or reroute: collision stops this purchased route and refunds untravelled steps.
        MovementSystem.CommitRoute(actor, path, keepFacing);
        return true;
    }
    public static bool Face(SimulationWorld world, string actorId, Point2 point, out string reason)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !world.Map.Inside(point)) { reason = "Choose a point on this map."; return false; }
        if (!Commit(world, actorId, CombatActionKind.Face, CombatState.FaceCost, 150, out reason, point: point)) return false;
        var actor = Actor(world, actorId);
        actor.Facing = ObservationService.FacingToward(actor.Position, point, actor.Facing); actor.KeepFacing = true;
        world.Anomalies.Evaluate(world, "tactical-facing");
        return true;
    }
    public static bool Equipment(SimulationWorld world, string actorId, CombatActionKind kind, out string reason)
    {
        if (!CanAct(world, actorId, out reason)) return false;
        var c = world.Combat.For(actorId)!;
        if (kind == CombatActionKind.ReadyWeapon && c.WeaponReady) { reason = "Training sidearm already ready."; return false; }
        if (kind == CombatActionKind.Reload && (c.Ammo == 6 || c.ReserveAmmo == 0)) { reason = "No reload needed or no reserve training rounds."; return false; }
        if (kind is not (CombatActionKind.ReadyWeapon or CombatActionKind.Reload)) { reason = "Unsupported equipment action."; return false; }
        return Commit(world, actorId, kind, kind == CombatActionKind.ReadyWeapon ? CombatState.ReadyCost : CombatState.ReloadCost,
            kind == CombatActionKind.ReadyWeapon ? 300 : 900, out reason);
    }
    public static bool Fire(SimulationWorld world, string actorId, string targetId, out string reason)
    {
        if (!CanAct(world, actorId, out reason)) return false;
        var state = world.Combat; var source = state.For(actorId)!; var target = state.For(targetId);
        var actor = Actor(world, actorId);
        if (target is null || target.Side == source.Side || target.Points == 0) { reason = "Choose an opposing training participant."; return false; }
        var victim = Actor(world, targetId);
        if (!source.WeaponReady || source.Ammo == 0) { reason = "Ready the training sidearm and load training rounds first."; return false; }
        if (actor.Cell.Distance(victim.Cell) > CombatState.WeaponHexes || !ObservationService.Inspect(world.Map, actor, victim.Position, world.LocalMs).Detected)
        { reason = "No visible target in training sidearm range."; return false; }
        if (!Commit(world, actorId, CombatActionKind.Fire, CombatState.AttackCost, 600, out reason, targetId, victim.Position)) return false;
        Replace(state, state.For(actorId)! with { Ammo = source.Ammo - 1 });
        actor.Facing = ObservationService.FacingToward(actor.Position, victim.Position, actor.Facing);
        world.Anomalies.Evaluate(world, "tactical-aim");
        return true;
    }
    public static bool Door(SimulationWorld world, string actorId, out string reason)
    {
        if (!DoorInteraction.Validate(world, Actor(world, actorId), null, out var door, out reason)) return false;
        if (door!.Reader is not null && !world.Map.OpenDoors.Contains(door.Id)
            && DoorCredentialService.Authorize(world, Actor(world, actorId), door, out var badge, out _) && badge is not null)
        {
            if (actorId != world.Player.Id) { reason = "Manual badge presentation requires this actor's handling controller."; return false; }
            return InventoryActions.Start(world, new(InventoryActionKind.PresentBadge, badge.Id, TargetId: door.Id), out reason);
        }
        return Commit(world, actorId, CombatActionKind.Door, 2, 300, out reason, door!.Id);
    }
    public static bool EndTurn(SimulationWorld world, string actorId, out string reason)
    {
        reason = "Finish the action and release any pause first.";
        var state = world.Combat;
        if (world.ManualPause || world.LocalHold || !state.Active || state.Pending is not null || state.CurrentActorId != actorId) return false;
        var cost = state.For(actorId)!.Ap;
        Replace(state, state.For(actorId)! with { Ap = 0 });
        state.Pending = new(state.NextActionId++, actorId, CombatActionKind.Wait, cost, 300, 300);
        reason = "Ending turn · 300ms local wait."; return true;
    }
    private static void AdvanceTurn(SimulationWorld world)
    {
        var state = world.Combat;
        do { state.Cursor++; if (state.Cursor == state.Roster.Count) { state.Cursor = 0; state.Round++; } }
        while (state.Roster[state.Cursor].Points == 0);
        BeginTurn(world);
    }
    public static bool FinishDrill(SimulationWorld world, out string reason, bool automatic = false)
    {
        var state = world.Combat;
        reason = "Finish the current action and wait for your turn to end the drill.";
        if (!state.Armed || !automatic && (world.ManualPause || world.LocalHold || state.Active && (state.Pending is not null || state.CurrentActorId != world.Player.Id))) return false;
        foreach (var actor in world.Actors) MovementSystem.StopInPlace(actor);
        state.Active = state.Armed = false; state.Pending = null;
        reason = "Training drill ended. Standing duties resume; training points are not injuries.";
        PlayerFeedback.Add(world, MessageKind.Status, reason); return true;
    }
    public static void ResolveTick(SimulationWorld world, Hex previousCell, Action? onResolvedPerception = null)
    {
        var state = world.Combat;
        if (state.Pending is not { } pending) return;
        var actor = Actor(world, pending.ActorId);
        pending = pending with { RemainingMs = pending.RemainingMs - SpatialSimulation.TickMs,
            CompletedSteps = pending.CompletedSteps + (actor.Cell != previousCell ? 1 : 0) };
        if (pending.Kind == CombatActionKind.Move)
            pending = pending with { RemainingMs = actor.Moving ? Math.Max(SpatialSimulation.TickMs, pending.RemainingMs) : 0 };
        state.Pending = pending;
        if (pending.RemainingMs > 0) return;
        var c = state.For(actor.Id)!;
        switch (pending.Kind)
        {
            case CombatActionKind.Move:
                var used = pending.CompletedSteps + (actor.StoppedStep is null ? 0 : 1);
                Replace(state, c with { Ap = Math.Min(CombatState.TurnAp, c.Ap + Math.Max(0, pending.CommittedSteps - used)) });
                break;
            case CombatActionKind.ReadyWeapon:
                Replace(state, c with { WeaponReady = true }); PlayerFeedback.Equipment(world, actor, actor == world.Player ? "ready a training sidearm." : "readies a training sidearm."); break;
            case CombatActionKind.Reload:
                var rounds = Math.Min(6 - c.Ammo, c.ReserveAmmo);
                Replace(state, c with { Ammo = c.Ammo + rounds, ReserveAmmo = c.ReserveAmmo - rounds });
                PlayerFeedback.Equipment(world, actor, actor == world.Player ? "reload training rounds." : "reloads training rounds."); break;
            case CombatActionKind.Door:
                if (DoorInteraction.Validate(world, actor, pending.TargetId, out var door, out var doorReason)
                    && (world.Map.OpenDoors.Contains(door!.Id) || door.Reader is null
                        || DoorCredentialService.Authorize(world, actor, door, out var held, out _) && held is null))
                { DoorInteraction.Toggle(world, door!); PlayerFeedback.Equipment(world, actor, actor == world.Player ? "operate the door." : "operates the door."); }
                else if (PlayerFeedback.Sees(world, actor)) PlayerFeedback.Add(world, MessageKind.Warning, $"Door use interrupted: {(doorReason == "accepted" ? "Visible badge is no longer available." : doorReason)}");
                break;
            case CombatActionKind.Fire:
                var victim = Actor(world, pending.TargetId!); var target = state.For(victim.Id)!;
                var eligible = actor.Cell.Distance(victim.Cell) <= CombatState.WeaponHexes && ObservationService.Inspect(world.Map, actor, victim.Position, world.LocalMs).Detected;
                var roll = state.Roll(); var hit = eligible && roll % 100 < 75;
                var damage = hit ? Math.Min(2, target.Points) : 0;
                Replace(state, target with { Points = target.Points - damage });
                PlayerFeedback.Attack(world, actor, victim, hit, damage);
                break;
        }
        // Door/equipment outcomes can change observation before the next input hold exists.
        world.Anomalies.Evaluate(world, "tactical-action-resolved");
        onResolvedPerception?.Invoke();
        PlayerFeedback.Sense(world);
        world.Log("combat", actor.Id, "action-resolved", $"#{pending.Id} {pending.Kind}");
        state.Pending = null;
        if (pending.Kind == CombatActionKind.Wait) AdvanceTurn(world);
        if (state.For(world.Player.Id)!.Points == 0 || !state.Roster.Any(p => p.Side == "opposition" && p.Points > 0)) FinishDrill(world, out _, true);
    }
}
