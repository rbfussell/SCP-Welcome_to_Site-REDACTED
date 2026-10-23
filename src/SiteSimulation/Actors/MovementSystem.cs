namespace SiteSimulation;

/// <summary>Physical movement/reservations, independent of sprites and SCP-specific rules.</summary>
internal static class MovementSystem
{
    private static readonly Hex[] Patrol = [new(5, 11), new(13, 7), new(13, 5), new(5, 9)];
    public static HashSet<Hex> Occupied(SimulationWorld world, ActorState except) => world.Actors.Where(a => a != except)
        .SelectMany(a => a.Moving ? new[] { a.Cell, a.Route[0] } : new[] { a.Cell }).ToHashSet();

    public static void Advance(SimulationWorld world)
    {
        foreach (var actor in world.Actors)
        {
            if (!actor.Moving || world.LocalMs < actor.StartledUntilMs) continue;
            if (!world.Map.CanTraverse(actor.Cell, actor.Route[0]) || Occupied(world, actor).Contains(actor.Route[0]))
            {
                actor.Route.Clear(); actor.SegmentMs = 0;
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
            patrol.Route = world.Map.FindPath(patrol.Cell, Patrol[world.PatrolIndex++ % Patrol.Length], Occupied(world, patrol)).ToList();
    }
}
