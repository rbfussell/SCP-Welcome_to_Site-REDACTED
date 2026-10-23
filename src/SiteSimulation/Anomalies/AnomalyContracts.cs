namespace SiteSimulation;

/// <summary>One authoritative spatial body; module state does not duplicate its location.</summary>
public sealed class AnomalyBody
{
    public string Id { get; }
    public string ModuleId { get; }
    public string ObserverProfile { get; }
    public int PoseCount { get; }
    public int MaxRelocationHexes { get; }
    public Hex Cell { get; internal set; }
    public Point2 Position => Cell.World;
    public int Pose { get; internal set; }
    public long Revision { get; internal set; }
    public AnomalyBody(string id, string moduleId, Hex cell, int poseCount, int maxRelocationHexes, string observerProfile)
    { Id = id; ModuleId = moduleId; Cell = cell; ObserverProfile = observerProfile; PoseCount = poseCount; MaxRelocationHexes = maxRelocationHexes; }
}
public sealed record AnomalyBodySnapshot(string Id, string ModuleId, Hex Cell, int Pose, long Revision);
public sealed record ModuleSnapshot(string InstanceId, string ModuleId, int Version, string Json);
public sealed record ModuleFlag(string Name, string ActorId, bool Enabled);
public sealed record AnomalyDiagnostics(string Id, string Rule, int Pose, int Relocations, int Candidates,
    long NextEvaluationMs, ObservationReport Observation, string[] RecentWitnesses, ModuleFlag[] Flags)
{
    public string[] FlaggedActors(string name) => Flags.Where(f => f.Name == name && f.Enabled).Select(f => f.ActorId).ToArray();
}
internal sealed record RelocationQuery(int MinHexes, int MaxHexes, bool BehindOnly, string NearObserverKind,
    string? NearActorId = null, bool RequireClearLine = false);
public sealed record RelocationCandidate(Hex Cell, string NearActorId, int DistanceHexes);

/// <summary>Bounded capabilities for this native prototype, not a public script sandbox.</summary>
internal interface IAnomalyWorld
{
    long LocalMs { get; }
    AnomalyBodySnapshot Body { get; }
    ObservationReport Observation(string profile);
    IReadOnlyList<RelocationCandidate> Candidates(string profile, RelocationQuery query);
    bool TryRelocate(long expectedRevision, Hex destination, int pose, string profile, RelocationQuery query, out string reason);
    void Witnessed(string actorId, int encounter);
    void Trace(string eventName, string detail);
}
internal interface IAnomalyModule
{
    string InstanceId { get; }
    string ModuleId { get; }
    bool AllowsObservedTransport { get; }
    void Evaluate(IAnomalyWorld world, string cause);
    AnomalyDiagnostics Inspect(IAnomalyWorld world);
    bool TrySetFlag(string flag, string actorId, string actorKind, bool enabled, out string reason);
    ModuleSnapshot Capture();
    void Restore(ModuleSnapshot snapshot, long localMs, IReadOnlyDictionary<string, string> actorKinds);
}
