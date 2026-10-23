namespace SiteSimulation;

/// <summary>Plan from the known facility layout and perceived occupants; execution still uses real collision.</summary>
internal static class PlayerNavigation
{
    private static (HashSet<Hex> Ignored, HashSet<Hex> Occupied) PlanningObstacles(SimulationWorld world)
    {
        bool Seen(Point2 point) => ObservationService.Inspect(world.Map, world.Player, point, world.LocalMs).Detected;
        var known = world.Anomalies.Bodies.Where(b => Seen(b.Position)).Select(b => b.Cell).ToHashSet();
        if (world.Operations.Definition is { } definition)
        {
            // The fixed terminal is on the known facility plan. Movable equipment needs actual sight.
            known.Add(definition.Terminal);
            if (Seen(world.Operations.Trolley.Position)) known.Add(Hex.FromWorld(world.Operations.Trolley.Position));
        }
        var occupied = world.Actors.Where(a => a != world.Player && Seen(a.Position))
            .SelectMany(a => a.StoppedStep is { } stopped ? new[] { a.Cell, stopped.Next }
                : a.Moving ? new[] { a.Cell, a.Route[0] } : new[] { a.Cell }).ToHashSet();
        // Anonymous collision/reservation cells are not evidence of a visible object.
        return (world.Map.DynamicBlockers.Except(known).ToHashSet(), occupied);
    }

    public static IReadOnlyList<Hex> PlanRoute(SimulationWorld world, Hex destination, IReadOnlyCollection<Hex>? bumped = null)
    {
        var obstacles = PlanningObstacles(world);
        obstacles.Occupied.UnionWith(bumped ?? world.Player.BumpedCells);
        var path = world.Map.FindPath(world.Player.Cell, destination, obstacles.Occupied, obstacles.Ignored, approachBlockedGoal: true);
        return MovementSystem.ContinueStopped(world, world.Player, destination, path);
    }

    public static bool KnownBlocked(SimulationWorld world, Hex cell)
    {
        var obstacles = PlanningObstacles(world);
        return !world.Map.Walkable(cell, obstacles.Ignored) || obstacles.Occupied.Contains(cell) || world.Player.BumpedCells.Contains(cell);
    }
}
