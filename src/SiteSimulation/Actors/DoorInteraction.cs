namespace SiteSimulation;

/// <summary>Shared physical checks for realtime use and paid tactical use.</summary>
internal static class DoorInteraction
{
    public static bool Validate(SimulationWorld world, ActorState actor, string? id, out WallDefinition? door, out string reason)
    {
        door = world.Map.Definition.Walls.Where(w => w.Door && (id is null || w.Id == id))
            .OrderBy(w => Point2.Distance(actor.Position, w.Center)).FirstOrDefault();
        reason = "Approach a doorway, then press E.";
        if (door is null || Point2.Distance(actor.Position, door.Center) > 1.35) return false;
        var selected = door;
        if (world.Map.OpenDoors.Contains(door.Id) && (world.Actors.Any(a => Geometry.DistanceToSegment(a.Position, selected.Start, selected.End) < 0.25
            || a.StoppedStep is { } stopped && Geometry.Intersects(a.Cell.World, stopped.Next.World, selected.Start, selected.End)
            || a.Moving && Geometry.Intersects(a.Position, a.Route[0].World, selected.Start, selected.End))
            || world.Anomalies.Bodies.Any(b => Geometry.DistanceToSegment(b.Cell.World, selected.Start, selected.End) < 0.18)))
        { reason = "Door obstructed by an actor, anomaly or committed crossing."; return false; }
        reason = "accepted"; return true;
    }
    public static void Toggle(SimulationWorld world, WallDefinition door)
    {
        if (!world.Map.OpenDoors.Remove(door.Id)) world.Map.OpenDoors.Add(door.Id);
        world.Log("world", door.Id, "door-changed", world.Map.OpenDoors.Contains(door.Id) ? "open" : "closed");
    }
}
