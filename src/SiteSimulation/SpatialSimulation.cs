namespace SiteSimulation;

/// <summary>Small orchestration facade. Rules live in named movement/perception/anomaly/save systems.</summary>
public sealed class SpatialSimulation
{
    public const int TickMs = 50;
    public const int StepDurationMs = 300;
    private readonly string _mapJson;
    private readonly string _mapIdentity;
    private SimulationWorld _world;
    public FacilityMap Map => _world.Map;
    public List<ActorState> Actors => _world.Actors;
    public ActorState Player => _world.Player;
    public AnomalySystem Anomalies => _world.Anomalies;
    public SimulationTrace Trace => _world.Trace;
    public PlayerAwareness Awareness => _world.Awareness;
    public long WorldMs => _world.WorldMs;
    public long LocalMs => _world.LocalMs;
    public bool LocalHold { get => _world.LocalHold; set { if (_world.LocalHold == value) return; _world.LocalHold = value; _world.Log("time", "local-zone", value ? "held" : "resumed", "World calendar continues; local progress retained."); } }
    public bool ManualPause { get => _world.ManualPause; set { if (_world.ManualPause == value) return; _world.ManualPause = value; _world.Log("time", "world", value ? "manual-pause" : "manual-resume", "Explicit single-player manual clock control."); } }
    public int Speed { get => _world.Speed; set => _world.Speed = value; }
    public int OffsiteUpdates => (int)(WorldMs / 1000);
    public int LocalCountdownMs => 10000 - (int)(LocalMs % 10000);
    public string LastMessage { get; private set; } = "SCP-650 is watched by Chen. O distracts Chen; K blinks; F8 changes lighting.";

    public SpatialSimulation(string json)
    {
        _mapJson = json;
        _mapIdentity = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
        _world = new(json);
        _world.Anomalies.Evaluate(_world, "initialize");
        EvaluatePlayerAwareness();
    }
    private bool Ready(string action)
    {
        if (_world.LocalActive) return true;
        LastMessage = action + " is held with local simulation.";
        return false;
    }
    private void Changed(string system, string subject, string action, string detail)
    {
        _world.Log(system, subject, action, detail);
        _world.Anomalies.Evaluate(_world, action + ":" + subject);
        LastMessage = detail;
        EvaluatePlayerAwareness();
    }
    private void EvaluatePlayerAwareness()
    {
        if (!_world.LocalActive) return;
        foreach (var body in Anomalies.Bodies)
        {
            // Bounded prototype communication: an audible, unobstructed nearby human report.
            // A remote observer is not a link. Radios/SCiPnet need their later qualified transport.
            foreach (var reporter in Actors.Where(a => a != Player && a.ObserverKind == "human"))
                if (Point2.Distance(reporter.Position, Player.Position) <= 4 && Map.ClearSight(reporter.Position, Player.Position)
                    && ObservationService.Inspect(Map, reporter, body.Cell.World, LocalMs).Detected)
                    ReceivePerceivedContact(new(body.Id, body.Revision, body.Cell.World, LocalMs, "voice", reporter.Id));
            if (VisibleToPlayer(body.Cell.World))
                ReceivePerceivedContact(new(body.Id, body.Revision, body.Cell.World, LocalMs, "direct", Player.Id));
        }
    }
    private void ReceivePerceivedContact(PerceivedContact evidence)
    {
        if (!Awareness.Receive(evidence)) return;
        _world.Log("awareness", evidence.SubjectId, "noticed", $"{evidence.Source} from {evidence.ObserverId}; encounter {evidence.Encounter}");
        if (!Player.Moving) return;
        MovementSystem.StopInPlace(Player);
        LastMessage = $"Movement stopped: anomaly {(evidence.Source == "direct" ? "sighted" : "reported")} · click to move again.";
        _world.Log("movement", Player.Id, "awareness-stop", $"{evidence.SubjectId}; {evidence.Source}; path cleared, fractional position retained; world continues.");
    }
    public IReadOnlyList<Hex> PreviewPath(Hex destination) => MovementSystem.PlanRoute(_world, Player, destination);
    public AnomalyDiagnostics[] InspectAnomalies() => Anomalies.Inspect(_world);
    public bool VisibleToPlayer(Point2 target) => ObservationService.Inspect(Map, Player, target, LocalMs).Detected;
    /// <summary>Native authoring/debug control. Not an in-world permission bypass for a public mod API.</summary>
    public bool SetModuleFlag(string instanceId, string flag, string actorId, bool enabled)
    {
        if (!Ready("Module flag")) return false;
        if (!Anomalies.TrySetFlag(_world, instanceId, flag, actorId, enabled, out var reason))
        { LastMessage = "Module flag rejected: " + reason; return false; }
        Changed("module-control", instanceId, "flag-changed", $"{flag}[{actorId}] = {enabled}; authoring/debug control.");
        return true;
    }

    public bool RequestMove(Hex destination, bool keepFacing = false)
    {
        if (!Ready("Movement")) return false;
        if (Player.Moving) { LastMessage = "Finish this route first (Esc cancels at the next hex)."; return false; }
        var path = PreviewPath(destination);
        if (path.Count == 0) { LastMessage = "No clear route: wall, closed door, occupied hex or anomaly body."; return false; }
        MovementSystem.CommitRoute(Player, path, keepFacing);
        Changed("movement", Player.Id, "route-committed", $"Route committed: {path.Count} hexes; {(keepFacing ? "maintain attention" : "face movement")}.");
        return true;
    }
    public void CancelMove()
    {
        if (!Ready("Cancellation")) return;
        Player.Route = Player.Route.Take(Player.SegmentMs > 0 ? 1 : 0).ToList();
        Changed("movement", Player.Id, "route-cancelled", "Route cancelled at next safe hex boundary.");
    }
    public void Face(Point2 target)
    {
        if (!Ready("Attention change")) return;
        Player.Facing = ObservationService.FacingToward(Player.Position, target, Player.Facing);
        Player.KeepFacing = true;
        Changed("perception", Player.Id, "facing-changed", "Attention changed independently of movement.");
    }
    public void ToggleObserverAttention()
    {
        if (!Ready("Observer fixture")) return;
        var observer = Actors.Single(a => a.Id == "observer"); observer.Attending = !observer.Attending;
        Changed("perception", observer.Id, "attention-changed", $"Chen {(observer.Attending ? "attending" : "distracted")}; prototype debug control.");
    }
    public void BlinkPlayer()
    {
        if (!Ready("Blink")) return;
        Player.EyesClosedUntilMs = LocalMs + 150;
        Changed("perception", Player.Id, "blink-started", "Avatar eyes closed for 150 local milliseconds.");
    }
    public void ToggleLighting()
    {
        if (!Ready("Lighting fixture")) return;
        Map.LightsOn = !Map.LightsOn;
        Changed("world", "facility", "lighting-changed", $"Prototype lighting {(Map.LightsOn ? "on" : "off")}.");
    }
    public bool ToggleNearestDoor()
    {
        if (!Ready("Door use")) return false;
        var door = Map.Definition.Walls.Where(w => w.Door).OrderBy(w => Point2.Distance(Player.Position, w.Center)).First();
        if (Point2.Distance(Player.Position, door.Center) > 1.35) { LastMessage = "Approach a doorway, then press E."; return false; }
        if (Map.OpenDoors.Contains(door.Id))
        {
            if (Actors.Any(a => Geometry.DistanceToSegment(a.Position, door.Start, door.End) < 0.25
                || a.StoppedStep is { } stopped && Geometry.Intersects(a.Cell.World, stopped.Next.World, door.Start, door.End)
                || a.Moving && Geometry.Intersects(a.Position, a.Route[0].World, door.Start, door.End))
                || Anomalies.Bodies.Any(b => Geometry.DistanceToSegment(b.Cell.World, door.Start, door.End) < 0.18))
            { LastMessage = "Door obstructed by an actor, anomaly or committed crossing."; return false; }
            Map.OpenDoors.Remove(door.Id);
        }
        else Map.OpenDoors.Add(door.Id);
        Changed("world", door.Id, "door-changed", $"{door.Id}: {(Map.OpenDoors.Contains(door.Id) ? "open" : "closed")}; path and sight updated.");
        return true;
    }
    public void Tick()
    {
        if (ManualPause) return;
        _world.WorldMs += TickMs;
        if (LocalHold) return;
        _world.LocalMs += TickMs;
        // Due perception transitions (including blink end) precede physical movement.
        Anomalies.Evaluate(_world, "local-timer");
        EvaluatePlayerAwareness();
        MovementSystem.Advance(_world);
        Anomalies.Evaluate(_world, "local-step-complete");
        EvaluatePlayerAwareness();
    }
    public string Save() => PrototypeSaveCodec.Capture(_world, _mapIdentity);
    public void Restore(string json)
    {
        // Decode/validate a detached candidate; failure cannot partially change live state.
        _world = PrototypeSaveCodec.Restore(json, _mapJson, _mapIdentity);
        LastMessage = "Restored movement, observers, module RNG/poses, local timers and trace.";
    }
}
