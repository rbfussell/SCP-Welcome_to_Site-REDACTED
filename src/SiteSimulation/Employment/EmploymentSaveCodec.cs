namespace SiteSimulation;

/// <summary>Detached employment restore validation. No duty execution or communication replay.</summary>
internal static class EmploymentSaveCodec
{
    public static void Restore(SimulationWorld world, EmploymentSnapshot? saved)
    {
        var state = world.Employment;
        if (state.Definition is not { } d)
        {
            if (saved is not null) throw new ArgumentException("Employment state has no matching definition.");
            return;
        }
        if (saved is null || saved.Duties is null || saved.Reports is null
            || saved.Duties.Any(s => s is null || !Enum.IsDefined(s.Phase) || !Enum.IsDefined(s.Release) || s.ChangedMs < 0 || s.ChangedMs > world.LocalMs
                || s.ChangedMs % SpatialSimulation.TickMs != 0 || string.IsNullOrWhiteSpace(s.Reason) || s.Reason.Length > 250
                || s.ReliefSinceMs is { } since && (since < 0 || since > world.LocalMs || since % SpatialSimulation.TickMs != 0)
                || s.Release != DutyRelease.None && s.Phase is not (DutyPhase.OffShift or DutyPhase.Blocked or DutyPhase.Interrupted)
                || s.Release is DutyRelease.Relieved or DutyRelease.OvertimeExpired && s.ReliefSinceMs is null)
            || !saved.Duties.Select(s => s.ActorId).Order().SequenceEqual(d.Appointments.Select(a => a.ActorId).Order())
            || saved.Reports.Length > d.Appointments.Length || saved.Reports.Any(r => r is null
                || !d.Appointments.Any(a => a.ActorId == r.ActorId) || !Enum.IsDefined(r.Phase)
                || r.LocalMs < 0 || r.LocalMs > world.LocalMs || r.LocalMs % SpatialSimulation.TickMs != 0
                || string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length > 250)
            || saved.Reports.Select(r => r.ActorId).Distinct().Count() != saved.Reports.Length)
            throw new ArgumentException("Invalid employment continuation or delivered reports.");
        state.Duties.Clear(); state.Duties.AddRange(saved.Duties);
        state.Reports.Clear(); state.Reports.AddRange(saved.Reports);
    }
}
