using System.Text.Json;

namespace SiteSimulation;

public sealed record OperationDefinition(string SubjectId, Hex Home, string[] AccessDoors, Hex[] Posts,
    Hex Terminal, Hex Trolley, ActorStart[] Starts, string[] RadioActors, string CommanderId, int StabilizeMs = 3000, bool FieldPhoneIssued = true, bool RegionalIntakeEnabled = false, EmploymentDefinition? Employment = null);
public sealed record ActorStart(string ActorId, Hex Cell, int Facing);
public enum PersonnelTask { Observe, Follow, Post, Transport, ResumeDuty }
public enum OrderStatus { Pending, Active, Blocked, Complete, Cancelled }
public sealed record PersonnelOrder(long Id, string Issuer, string ActorId, PersonnelTask Task, Hex Destination,
    string Channel, OrderStatus Status, string Detail, long IssuedMs, Point2? TargetPoint = null);
public sealed record StaffContact(string ActorId, string SubjectId, Point2 Position, long Encounter, long LocalMs);
public sealed record OperationReport(long Id, long LocalMs, string Author, string Text, Point2? Position = null,
    string? SubjectId = null, long Encounter = 0, string Source = "system");
public sealed record RadioState(string ActorId, bool Powered = true);
public sealed record TrolleyStep(Hex Next, Hex HandlerNext, int ProgressMs, string Stage = "push");
public sealed record TrolleyState(Point2 Position, string? SubjectId = null, string? HandlerId = null, TrolleyStep? Step = null);
public sealed record OperationSnapshot(long NextId, PersonnelOrder[] Orders, StaffContact[] Contacts, OperationReport[] Reports,
    RadioState[] Radios, bool TerminalPowered, bool NetworkOnline, TrolleyState Trolley, int StableMs, bool HadBreach,
    bool Completed, string PhysicalStatus, FieldPhoneState? Phone = null, long[]? AcquiredReports = null, bool RegionalIntakeSent = false);

/// <summary>Serializable operation facts; orders, transport and terminal projections have separate services.</summary>
public sealed class OperationState
{
    public OperationDefinition? Definition { get; }
    internal List<PersonnelOrder> Orders { get; } = [];
    internal List<StaffContact> Contacts { get; } = [];
    internal List<OperationReport> Reports { get; } = [];
    public IReadOnlyList<PersonnelOrder> OrderHistory => Orders.AsReadOnly();
    public IReadOnlyList<OperationReport> ReceivedReports => Reports.AsReadOnly();
    internal bool RegionalIntakeSent { get; set; }
    public FieldPhoneState? Phone { get; set; }
    internal HashSet<long> AcquiredReports { get; } = [];
    public List<RadioState> Radios { get; } = [];
    public bool TerminalPowered { get; set; } = true;
    public bool NetworkOnline { get; set; } = true;
    public TrolleyState Trolley { get; internal set; } = new(new(0, 0));
    public int StableMs { get; internal set; }
    public bool HadBreach { get; internal set; }
    public bool Completed { get; internal set; }
    public string PhysicalStatus { get; internal set; } = "Awaiting coverage";
    internal long NextId { get; set; } = 1;
    internal OperationState(string? json)
    {
        if (json is null) return;
        Definition = JsonSerializer.Deserialize<OperationDefinition>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Empty operation definition.");
        Phone = new(Definition.CommanderId, Definition.FieldPhoneIssued);
        Trolley = new(Definition.Trolley.World);
        Radios.AddRange(Definition.RadioActors.Select(id => new RadioState(id)));
    }
    internal void Report(SimulationWorld world, string author, string text, Point2? position = null,
        string? subjectId = null, long encounter = 0, string source = "system")
    {
        Reports.Add(new(NextId++, world.LocalMs, author, text, position, subjectId, encounter, source));
        if (Reports.Count > 40) { AcquiredReports.Remove(Reports[0].Id); Reports.RemoveAt(0); }
        world.Log("communications", author, "delivered", text);
    }
    internal OperationSnapshot Capture() => new(NextId, Orders.ToArray(), Contacts.ToArray(), Reports.ToArray(), Radios.ToArray(),
        TerminalPowered, NetworkOnline, Trolley, StableMs, HadBreach, Completed, PhysicalStatus, Phone, AcquiredReports.Order().ToArray(), RegionalIntakeSent);
}
