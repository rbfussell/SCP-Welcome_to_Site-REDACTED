namespace SiteSimulation;

public sealed record WallDefinition(string Id, double X, double Y, string Axis, bool Door = false)
{
    public Point2 Start => new(X, Y);
    public Point2 End => new(X + (Axis == "u" ? 1 : 0), Y + (Axis == "v" ? 1 : 0));
    public Point2 Center => (Start + End) * 0.5;
}
public sealed record PropDefinition(string Id, string Kind, int Q, int R, string Label)
{
    public Hex Cell => new(Q, R);
}
public sealed record ActorDefinition(string Id, string Name, string Kind, int Q, int R, int Facing, string ObserverKind = "human");
public sealed record MapDefinition(int Version, int Width, int Height, WallDefinition[] Walls, PropDefinition[] Props, ActorDefinition[] Actors, AnomalyDefinition[]? Anomalies = null);

public sealed record AnomalyDefinition(string Id, string Module, int Q, int R, string ObserverProfile = "human", uint Seed = 650);
