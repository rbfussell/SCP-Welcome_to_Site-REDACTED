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
        actor.MoveGoal = null; actor.BumpedCells.Clear();
    }

    public static IReadOnlyList<Hex> PlanRoute(SimulationWorld world, ActorState actor, Hex destination)
    {
        var path = world.Map.FindPath(actor.Cell, destination, Occupied(world, actor));
        return ContinueStopped(world, actor, destination, path);
    }

    internal static IReadOnlyList<Hex> ContinueStopped(SimulationWorld world, ActorState actor, Hex destination, IReadOnlyList<Hex> path)
    {
        if (actor.StoppedStep is not { } stopped) return path;
        if (!world.Map.CanTraverse(actor.Cell, stopped.Next) || destination != actor.Cell && path.Count == 0) return [];
        return path.Count > 0 && path[0] == stopped.Next ? path : new[] { actor.Cell }.Concat(path).ToArray();
    }

    public static void CommitRoute(ActorState actor, IReadOnlyList<Hex> path, bool keepFacing,
        Hex? moveGoal = null, IReadOnlyCollection<Hex>? bumped = null)
    {
        if (path.Count == 0) return;
        if (actor.StoppedStep is { } stopped)
        {
            if (path[0] == stopped.Next) actor.SegmentMs = stopped.ProgressMs;
            else { actor.Cell = stopped.Next; actor.SegmentMs = SpatialSimulation.StepDurationMs - stopped.ProgressMs; }
            actor.StoppedStep = null;
        }
        actor.Route = path.ToList(); actor.KeepFacing = keepFacing;
        actor.MoveGoal = moveGoal; actor.BumpedCells = bumped?.ToList() ?? [];
    }

    public static bool Advance(SimulationWorld world, string? onlyActor = null)
    {
        var playerBlocked = false;
        foreach (var actor in world.Actors)
        {
            if (onlyActor is not null && actor.Id != onlyActor || !actor.Moving || world.LocalMs < actor.StartledUntilMs) continue;
            if (!world.Map.CanTraverse(actor.Cell, actor.Route[0]) || Occupied(world, actor).Contains(actor.Route[0]))
            {
                var goal = actor.MoveGoal;
                var bumped = actor.BumpedCells.Append(actor.Route[0]).Distinct().ToArray();
                StopInPlace(actor);
                if (actor == world.Player && goal is { } destination)
                {
                    var reroute = PlayerNavigation.PlanRoute(world, destination, bumped);
                    if (reroute.Count > 0)
                    {
                        CommitRoute(actor, reroute, actor.KeepFacing, destination, bumped);
                        world.Log("movement", actor.Id, "rerouted", "Encountered an obstruction; alternate route to the same destination.");
                        continue;
                    }
                }
                world.Log("movement", actor.Id, "blocked", "Next step blocked; remaining route cancelled.");
                if (actor == world.Player) playerBlocked = true;
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
            if (!actor.Moving && actor.MoveGoal is { } requested)
            {
                if (actor.Cell != requested)
                {
                    var continuation = PlayerNavigation.PlanRoute(world, requested);
                    if (continuation.Count > 0)
                        CommitRoute(actor, continuation, actor.KeepFacing, requested, actor.BumpedCells.ToArray());
                    else
                    {
                        StopInPlace(actor); playerBlocked = true;
                        world.Log("movement", actor.Id, "blocked", "Reached the blocking boundary; no closer route to the requested destination.");
                    }
                }
                else { actor.MoveGoal = null; actor.BumpedCells.Clear(); }
            }
        }
        var patrol = world.Actors.Single(a => a.Id == "tech");
        if (onlyActor is null && !PersonnelOrderSystem.Owns(world.Operations, patrol.Id) && !EmploymentSystem.Owns(world, patrol.Id) && !patrol.Moving && world.LocalMs % 1000 == 0 && world.LocalMs >= patrol.StartledUntilMs)
            CommitRoute(patrol, PlanRoute(world, patrol, Patrol[world.PatrolIndex++ % Patrol.Length]), false);
        return playerBlocked;
    }
}
