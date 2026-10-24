namespace SiteSimulation;

/// <summary>Deterministic training decisions use direct observations and retained points, never hidden live targets.</summary>
internal static class TrainingOpponentSystem
{
    public static void Observe(SimulationWorld world)
    {
        var state = world.Combat;
        foreach (var c in state.Participants.Where(c => c.Points > 0))
        {
            var actor = world.Actors.Single(a => a.Id == c.ActorId);
            foreach (var target in state.Participants.Where(t => t.Side != c.Side && t.Points > 0))
            {
                var other = world.Actors.Single(a => a.Id == target.ActorId);
                if (!ObservationService.Inspect(world.Map, actor, other.Position, world.LocalMs).Detected) continue;
                state.Contacts.RemoveAll(p => p.ObserverId == actor.Id && p.TargetId == other.Id);
                state.Contacts.Add(new(actor.Id, other.Id, other.Position, world.LocalMs));
            }
        }
    }
    public static void Choose(SimulationWorld world)
    {
        var state = world.Combat;
        if (!state.Active || state.Pending is not null || state.CurrentActorId == world.Player.Id) return;
        var actor = world.Actors.Single(a => a.Id == state.CurrentActorId);
        var c = state.For(actor.Id)!;
        if (world.LocalMs < actor.StartledUntilMs || c.Ap == 0) { TacticalCombatSystem.EndTurn(world, actor.Id, out _); return; }
        var target = state.Contacts.Where(p => p.ObserverId == actor.Id && state.For(p.TargetId)!.Points > 0)
            .OrderBy(p => Point2.Distance(actor.Position, p.Position)).ThenBy(p => p.TargetId, StringComparer.Ordinal).FirstOrDefault();
        if (target is not null)
        {
            if (!c.WeaponReady && TacticalCombatSystem.Equipment(world, actor.Id, CombatActionKind.ReadyWeapon, out _)) return;
            if (c.Ammo == 0 && TacticalCombatSystem.Equipment(world, actor.Id, CombatActionKind.Reload, out _)) return;
            // Fire revalidates actual direct sight. Retained contact is only a travel/facing cue.
            if (TacticalCombatSystem.Fire(world, actor.Id, target.TargetId, out _)) return;
            if (actor.Cell != Hex.FromWorld(target.Position))
            {
                var route = MovementSystem.PlanRoute(world, actor, Hex.FromWorld(target.Position));
                // A visible target occupies its cell: approach a reachable neighbour instead.
                if (route.Count == 0) route = Enumerable.Range(0, 6).Select(Hex.FromWorld(target.Position).Neighbor)
                    .Select(cell => MovementSystem.PlanRoute(world, actor, cell)).Where(p => p.Count > 0).OrderBy(p => p.Count).FirstOrDefault() ?? [];
                if (route.Count > 0 && TacticalCombatSystem.Move(world, actor.Id, route[Math.Min(Math.Min(3, c.Ap), route.Count) - 1], false, out _)) return;
            }
        }
        if (TacticalCombatSystem.Face(world, actor.Id, actor.Cell.Neighbor((actor.Facing + 1) % 6).World, out _)) return;
        TacticalCombatSystem.EndTurn(world, actor.Id, out _);
    }
}
