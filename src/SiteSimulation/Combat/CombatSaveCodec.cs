namespace SiteSimulation;

internal static class CombatSaveCodec
{
    public static void Restore(SimulationWorld world, CombatSnapshot s)
    {
        var actors = world.Actors.Select(a => a.Id).ToHashSet();
        if (s is null || s.Participants is null || s.Contacts is null || s.NextActionId < 1 || s.RandomState == 0
            || s.Participants.Length > actors.Count || s.Participants.Any(c => c is null || !actors.Contains(c.ActorId)
                || c.Side is not ("friendly" or "opposition") || c.Initiative is < 1 or > 20 || c.Ap is < 0 or > CombatState.TurnAp
                || c.Points is < 0 or > 6 || c.Ammo is < 0 or > 6 || c.ReserveAmmo is < 0 or > 12)
            || s.Participants.Select(c => c.ActorId).Distinct().Count() != s.Participants.Length
            || s.Cursor < 0 || s.Round < 0 || s.Active && (!s.Armed || s.Round < 1)
            || s.Armed && (s.Participants.Length != actors.Count || s.Cursor >= s.Participants.Length
                || s.Participants.Count(c => c.Side == "opposition") != 1 || s.Participants.Single(c => c.ActorId == world.Player.Id).Side != "friendly")
            || s.Participants.Length > 0 && s.Cursor >= s.Participants.Length
            || s.Active && (s.Participants[s.Cursor].Points == 0 || s.Participants.Single(c => c.ActorId == world.Player.Id).Points == 0
                || !s.Participants.Any(c => c.Side == "opposition" && c.Points > 0))
            || s.Contacts.Length > actors.Count * actors.Count
            || s.Contacts.Any(c => c is null || !s.Participants.Any(p => p.ActorId == c.ObserverId)
                || !s.Participants.Any(p => p.ActorId == c.TargetId) || c.ObserverId == c.TargetId
                || !double.IsFinite(c.Position.X) || !double.IsFinite(c.Position.Y) || !world.Map.Inside(c.Position)
                || c.LocalMs < 0 || c.LocalMs > world.LocalMs)
            || s.Contacts.Select(c => (c.ObserverId, c.TargetId)).Distinct().Count() != s.Contacts.Length)
            throw new ArgumentException("Invalid tactical encounter.");
        if (s.Pending is { } action)
        {
            if (!s.Active || action.ActorId != s.Participants[s.Cursor].ActorId || action.Id < 1 || action.Id >= s.NextActionId
                || !Enum.IsDefined(action.Kind) || action.Cost is < 0 or > CombatState.TurnAp || action.Cost + s.Participants[s.Cursor].Ap > CombatState.TurnAp
                || action.TotalMs <= 0 || action.TotalMs % SpatialSimulation.TickMs != 0
                || action.RemainingMs <= 0 || action.RemainingMs > action.TotalMs || action.RemainingMs % SpatialSimulation.TickMs != 0
                || action.TargetPoint is { } point && (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !world.Map.Inside(point)))
                throw new ArgumentException("Invalid tactical continuation.");
            var valid = action.Kind switch
            {
                CombatActionKind.Move => action.CommittedSteps == action.Cost && action.Cost > 0 && action.CompletedSteps >= 0
                    && action.CompletedSteps < action.CommittedSteps && world.Actors.Single(a => a.Id == action.ActorId).Moving
                    && world.Actors.Single(a => a.Id == action.ActorId).Route.Count + action.CompletedSteps == action.CommittedSteps && action.TotalMs <= action.CommittedSteps * SpatialSimulation.StepDurationMs
                    && action.TotalMs > (action.CommittedSteps - 1) * SpatialSimulation.StepDurationMs && action.TargetPoint is not null,
                CombatActionKind.Fire => action.Cost == CombatState.AttackCost && action.TotalMs == 600
                    && s.Participants.Any(c => c.ActorId == action.TargetId && c.Side != s.Participants[s.Cursor].Side && c.Points > 0)
                    && s.Participants[s.Cursor].WeaponReady && action.TargetPoint is not null,
                CombatActionKind.Face => action.Cost == CombatState.FaceCost && action.TotalMs == 150 && action.TargetPoint is not null,
                CombatActionKind.ReadyWeapon => action.Cost == CombatState.ReadyCost && action.TotalMs == 300 && !s.Participants[s.Cursor].WeaponReady,
                CombatActionKind.Reload => action.Cost == CombatState.ReloadCost && action.TotalMs == 900
                    && s.Participants[s.Cursor].Ammo < 6 && s.Participants[s.Cursor].ReserveAmmo > 0,
                CombatActionKind.Door => action.Cost == 2 && action.TotalMs == 300 && world.Map.Definition.Walls.Any(w => w.Id == action.TargetId && w.Door),
                CombatActionKind.Wait => action.TotalMs == 300 && s.Participants[s.Cursor].Ap == 0,
                _ => false
            };
            if (!valid || action.Kind != CombatActionKind.Move && (action.CommittedSteps != 0 || action.CompletedSteps != 0))
                throw new ArgumentException("Invalid tactical action cost or target.");
        }
        if (s.Active && world.Actors.Any(a => a.Moving && (s.Pending?.Kind != CombatActionKind.Move || s.Pending.ActorId != a.Id)
            || a.MoveGoal is not null)) throw new ArgumentException("Unscheduled tactical movement.");
        var state = world.Combat;
        state.Armed = s.Armed; state.Active = s.Active; state.Roster.Clear(); state.Roster.AddRange(s.Participants);
        state.Contacts.Clear(); state.Contacts.AddRange(s.Contacts);
        state.Cursor = s.Cursor; state.Round = s.Round; state.Pending = s.Pending; state.NextActionId = s.NextActionId; state.RandomState = s.RandomState;
    }
}
