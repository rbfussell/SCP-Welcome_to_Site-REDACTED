using System.Text.Json;

namespace SiteSimulation;

internal sealed record SpatialSnapshot(int Version, string MapIdentity, long WorldMs, long LocalMs, bool LocalHold, bool ManualPause,
    int Speed, string[] OpenDoors, ActorState[] Actors, int PatrolIndex, bool LightsOn,
    AnomalyBodySnapshot[] Bodies, ModuleSnapshot[] Modules, TraceSnapshot Trace, AwarenessSnapshot? Awareness = null);

/// <summary>Explicit schema and detached restore transaction. Earlier prototype saves are rejected.</summary>
internal static class PrototypeSaveCodec
{
    public static string Capture(SimulationWorld world, string identity) => JsonSerializer.Serialize(new SpatialSnapshot(3, identity,
        world.WorldMs, world.LocalMs, world.LocalHold, world.ManualPause, world.Speed, world.Map.OpenDoors.Order().ToArray(),
        world.Actors.ToArray(), world.PatrolIndex, world.Map.LightsOn, world.Anomalies.CaptureBodies(), world.Anomalies.CaptureModules(), world.Trace.Capture(), world.Awareness.Capture()));

    public static SimulationWorld Restore(string json, string mapJson, string identity)
    {
        var s = JsonSerializer.Deserialize<SpatialSnapshot>(json) ?? throw new ArgumentException("Empty prototype save.");
        if (s.Version is not (2 or 3) || s.Version == 3 && s.Awareness is null || s.MapIdentity != identity || s.WorldMs < 0 || s.LocalMs < 0 || s.LocalMs > s.WorldMs
            || s.WorldMs % SpatialSimulation.TickMs != 0 || s.LocalMs % SpatialSimulation.TickMs != 0
            || s.Speed is not (1 or 2 or 4) || s.PatrolIndex < 0 || s.Actors is null || s.OpenDoors is null || s.Actors.Any(a => a is null))
            throw new ArgumentException("Incompatible or invalid prototype save (requires schema 2/3 and matching content).");
        var candidate = new SimulationWorld(mapJson);
        if (!s.Actors.Select(a => a.Id).Order().SequenceEqual(candidate.Actors.Select(a => a.Id).Order())
            || s.OpenDoors.Any(id => !candidate.Map.Definition.Walls.Any(w => w.Id == id && w.Door))
            || s.Actors.Any(a => a.Route is null || a.WalkMs < 0 || a.SegmentMs < 0 || a.SegmentMs >= SpatialSimulation.StepDurationMs
                || a.SegmentMs % SpatialSimulation.TickMs != 0 || a.Facing is < 0 or > 5
                || a.EyesClosedUntilMs < 0 || a.EyesClosedUntilMs > s.LocalMs + 150
                || a.StartledUntilMs < 0 || a.StartledUntilMs > s.LocalMs + 400
                || !candidate.Map.Definition.Actors.Any(d => d.Id == a.Id && d.Kind == a.Kind && d.Name == a.Name && d.ObserverKind == a.ObserverKind)))
            throw new ArgumentException("Invalid saved actor, door or continuation.");
        candidate.Actors = s.Actors.ToList();
        candidate.WorldMs = s.WorldMs; candidate.LocalMs = s.LocalMs; candidate.Speed = s.Speed;
        candidate.LocalHold = s.LocalHold; candidate.ManualPause = s.ManualPause; candidate.PatrolIndex = s.PatrolIndex;
        candidate.Map.OpenDoors.UnionWith(s.OpenDoors); candidate.Map.LightsOn = s.LightsOn;
        candidate.Anomalies.Restore(s.Bodies, s.Modules, candidate);
        foreach (var actor in candidate.Actors)
        {
            if (!candidate.Map.Walkable(actor.Cell) || actor.Route.Count == 0 && actor.SegmentMs != 0)
                throw new ArgumentException("Save overlaps an actor or loses movement continuation.");
            if (actor.SegmentMs > 0 && !candidate.Map.CanTraverse(actor.Cell, actor.Route[0]))
                throw new ArgumentException("Saved in-progress step crosses blocked geometry.");
            if (actor.StoppedStep is { } stopped && (s.Version < 3 || actor.Moving || actor.SegmentMs != 0
                || stopped.ProgressMs is <= 0 or >= SpatialSimulation.StepDurationMs || stopped.ProgressMs % SpatialSimulation.TickMs != 0
                || !candidate.Map.CanTraverse(actor.Cell, stopped.Next)
                || MovementSystem.Occupied(candidate, actor).Contains(stopped.Next)))
                throw new ArgumentException("Invalid stopped fractional movement state.");
            var previous = actor.Cell;
            foreach (var next in actor.Route)
            {
                if (previous.Distance(next) != 1 || !candidate.Map.Inside(next.World)) throw new ArgumentException("Invalid saved path.");
                previous = next;
            }
        }
        if (candidate.Actors.Select(a => a.Cell).Distinct().Count() != candidate.Actors.Count)
            throw new ArgumentException("Save overlaps actors.");
        candidate.Trace.Restore(s.Trace, s.WorldMs, s.LocalMs);
        var awareness = s.Version == 2 ? new AwarenessSnapshot(true, true, [], []) : s.Awareness!;
        if (awareness.Evidence is not null && awareness.Evidence.Any(e => e is not null && (!candidate.Map.Inside(e.Position)
            || e.Source == "direct" && e.ObserverId != candidate.Player.Id)))
            throw new ArgumentException("Invalid local contact position or direct observer.");
        candidate.Awareness.Restore(awareness, candidate.Anomalies.Bodies.ToDictionary(b => b.Id, b => b.Revision),
            candidate.Actors.Select(a => a.Id).ToHashSet(), candidate.LocalMs);
        return candidate;
    }
}
