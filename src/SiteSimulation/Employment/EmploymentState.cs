namespace SiteSimulation;

/// <summary>Native first-slice employment definitions. Shift clocks use local execution time.</summary>
public sealed record EmploymentDefinition(DutyPost[] Posts, StaffAppointment[] Appointments);
public sealed record DutyPost(string Id, string Name, Point2 LookAt, int MinimumStaff = 1);
public sealed record StaffAppointment(string ActorId, string Role, string SupervisorId, string PostId,
    Hex Station, Hex Rest, int RestFacing, int CycleMs, int StartMs, int DurationMs, int MaxOvertimeMs = 20000);
public enum DutyPhase { Reporting, OnDuty, AwaitingRelief, OffShift, Interrupted, Blocked }
public enum DutyRelease { None, OffShift, Relieved, OvertimeExpired }
public sealed record StaffDuty(string ActorId, DutyPhase Phase, string Reason, long ChangedMs,
    long? ReliefSinceMs = null, DutyRelease Release = DutyRelease.None);
public sealed record StaffDutyReport(string ActorId, DutyPhase Phase, string Reason, long LocalMs);
public sealed record EmploymentSnapshot(StaffDuty[] Duties, StaffDutyReport[] Reports);

/// <summary>Authoritative work state and separately delivered staff reports; no widget-owned duties.</summary>
public sealed class EmploymentState
{
    public EmploymentDefinition? Definition { get; }
    internal List<StaffDuty> Duties { get; } = [];
    internal List<StaffDutyReport> Reports { get; } = [];
    public IReadOnlyList<StaffDuty> DiagnosticDuties => Duties.AsReadOnly();
    public IReadOnlyList<StaffDutyReport> DeliveredReports => Reports.AsReadOnly();
    internal EmploymentState(EmploymentDefinition? definition) => Definition = definition;
    internal EmploymentSnapshot Capture() => new(Duties.ToArray(), Reports.ToArray());
}
