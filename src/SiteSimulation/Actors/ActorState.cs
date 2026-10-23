namespace SiteSimulation;

/// <summary>A stopped fractional edge, retained without a path or active destination.</summary>
public sealed record StoppedStep(Hex Next, int ProgressMs);

/// <summary>Serializable actor facts. Movement and perception services interpret them.</summary>
public sealed class ActorState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string ObserverKind { get; set; } = "human";
    public bool Attending { get; set; } = true;
    public long EyesClosedUntilMs { get; set; }
    public long StartledUntilMs { get; set; }
    public Hex Cell { get; set; }
    public List<Hex> Route { get; set; } = [];
    public int SegmentMs { get; set; }
    public int Facing { get; set; }
    public bool KeepFacing { get; set; }
    public long WalkMs { get; set; }
    public StoppedStep? StoppedStep { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Point2 Position => StoppedStep is { } stopped
        ? Cell.World + (stopped.Next.World - Cell.World) * (stopped.ProgressMs / (double)SpatialSimulation.StepDurationMs)
        : Route.Count == 0 ? Cell.World : Cell.World + (Route[0].World - Cell.World) * (SegmentMs / (double)SpatialSimulation.StepDurationMs);
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Moving => Route.Count > 0;
}
