namespace SiteSimulation;

/// <summary>Physical movement/reservations, independent of sprites and SCP-specific rules.</summary>
internal static class MovementSystem
{
    private static readonly Hex[] Patrol = [new(5, 11), new(13, 7), new(13, 5), new(5, 9)];
    public static HashSet<Hex> Occupied(SimulationWorld world, ActorState except) => world.Actors.Where(a => a != except)
        .SelectMany(a => a.StoppedStep is { } stopped ? new[] { a.Cell, stopped.Next }
            : a.Moving ? new[] { a.Cell, a.Route[0] } : new[] { a.Cell }).ToHashSet();

    public static void StopInPlace(ActorState actor)
    {
        if (actor.SegmentMs > 0 && actor.Route.Count > 0) actor.StoppedStep = new(actor.Route[0], actor.SegmentMs);
        actor.Route.Clear(); actor.SegmentMs = 0;
    }

    public static IReadOnlyList<Hex> PlanRoute(SimulationWorld world, ActorState actor, Hex destination)
    {
        var path = world.Map.FindPath(actor.Cell, destination, Occupied(world, actor));
        if (actor.StoppedStep is not { } stopped) return path;
        if (!world.Map.CanTraverse(actor.Cell, stopped.Next) || destination != actor.Cell && path.Count == 0) return [];
        return path.Count > 0 && path[0] == stopped.Next ? path : new[] { actor.Cell }.Concat(path).ToArray();
    }

    public static void CommitRoute(ActorState actor, IReadOnlyList<Hex> path, bool keepFacing)
    {
        if (path.Count == 0) return;
        if (actor.StoppedStep is { } stopped)
        {
            if (path[0] == stopped.Next) actor.SegmentMs = stopped.ProgressMs;
            else { actor.Cell = stopped.Next; actor.SegmentMs = SpatialSimulation.StepDurationMs - stopped.ProgressMs; }
            actor.StoppedStep = null;
        }
        actor.Route = path.ToList(); actor.KeepFacing = keepFacing;
    }

    public static void Advance(SimulationWorld world)
    {
        foreach (var actor in world.Actors)
        {
            if (!actor.Moving || world.LocalMs < actor.StartledUntilMs) continue;
            if (!world.Map.CanTraverse(actor.Cell, actor.Route[0]) || Occupied(world, actor).Contains(actor.Route[0]))
            {
                StopInPlace(actor);
                world.Log("movement", actor.Id, "blocked", "Next step blocked; remaining route cancelled.");
                continue;
            }
            if (!actor.KeepFacing) actor.Facing = Array.FindIndex(Hex.Directions,
                d => d.Q == actor.Route[0].Q - actor.Cell.Q && d.R == actor.Route[0].R - actor.Cell.R);
            actor.SegmentMs += SpatialSimulation.TickMs;
            actor.WalkMs += SpatialSimulation.TickMs;
            if (actor.SegmentMs >= SpatialSimulation.StepDurationMs)
            {
                actor.Cell = actor.Route[0]; actor.Route.RemoveAt(0); actor.SegmentMs = 0;
            }
            // Preserve each observer transition rather than sampling only after all actors move.
            world.Anomalies.Evaluate(world, "movement:" + actor.Id);
        }
        var patrol = world.Actors.Single(a => a.Id == "tech");
        if (!patrol.Moving && world.LocalMs % 1000 == 0 && world.LocalMs >= patrol.StartledUntilMs)
            CommitRoute(patrol, PlanRoute(world, patrol, Patrol[world.PatrolIndex++ % Patrol.Length]), false);
    }
}
