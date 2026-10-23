namespace SiteSimulation;

/// <summary>Authoritative prototype data. Systems receive this state; the facade schedules them.</summary>
internal sealed class SimulationWorld
{
    public FacilityMap Map { get; }
    public List<ActorState> Actors { get; set; }
    public AnomalySystem Anomalies { get; }
    public SimulationTrace Trace { get; } = new();
    public long WorldMs { get; set; }
    public long LocalMs { get; set; }
    public bool LocalHold { get; set; }
    public bool ManualPause { get; set; }
    public int Speed { get; set; } = 1;
    public int PatrolIndex { get; set; }
    public bool LocalActive => !LocalHold && !ManualPause;
    public ActorState Player => Actors.Single(a => a.Kind == "player");

    public SimulationWorld(string mapJson)
    {
        Map = FacilityMap.Load(mapJson);
        Actors = Map.Definition.Actors.Select(a => new ActorState
        {
            Id = a.Id, Name = a.Name, Kind = a.Kind, ObserverKind = a.ObserverKind,
            Cell = new(a.Q, a.R), Facing = a.Facing
        }).ToList();
        Anomalies = new AnomalySystem(Map.Definition.Anomalies ?? []);
        Anomalies.RegisterBlockers(Map);
        if (Actors.Any(a => !Map.Walkable(a.Cell))) throw new ArgumentException("Anomaly overlaps an actor spawn.");
    }
    public void Log(string system, string subject, string eventName, string detail) => Trace.Add(WorldMs, LocalMs, system, subject, eventName, detail);
}
