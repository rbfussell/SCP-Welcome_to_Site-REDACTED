using System.Text.Json;

namespace SiteSimulation;

/// <summary>Authored slice geometry. Collision and sight never consult sprite pixels.</summary>
public sealed class FacilityMap
{
    public MapDefinition Definition { get; }
    public HashSet<string> OpenDoors { get; } = [];
    public Hex[] Cells { get; }
    public bool LightsOn { get; set; } = true;
    public HashSet<Hex> DynamicBlockers { get; } = [];
    public IEnumerable<WallDefinition> SolidWalls => Definition.Walls.Where(w => !w.Door || !OpenDoors.Contains(w.Id));

    public FacilityMap(MapDefinition definition)
    {
        if (definition.Walls is null || definition.Props is null || definition.Actors is null)
            throw new ArgumentException("Map requires walls, props and actors arrays.");
        if (definition.Actors.Any(a => a.Facing is < 0 or > 5 || a.ObserverKind is not ("human" or "ai")))
            throw new ArgumentException("Actor facing/observer kind is unsupported by this prototype.");
        if (definition.Version != 1 || definition.Width is < 1 or > 64 || definition.Height is < 1 or > 64)
            throw new ArgumentException("Unsupported map version or dimensions.");
        if (definition.Walls.Any(w => w.Axis is not ("u" or "v"))) throw new ArgumentException("Invalid wall axis.");
        if (definition.Walls.Any(w => w.Reader is { } reader && (!w.Door || string.IsNullOrWhiteSpace(reader.Issuer)
            || reader.Issuer.Length > 100 || reader.Clearance is < 0 or > 5)))
            throw new ArgumentException("Invalid door reader policy.");
        var ids = definition.Walls.Select(w => w.Id).Concat(definition.Props.Select(p => p.Id)).Concat(definition.Actors.Select(a => a.Id)).Concat((definition.Anomalies ?? []).Select(a => a.Id)).ToArray();
        if (ids.Distinct().Count() != ids.Length) throw new ArgumentException("Duplicate fixture identity.");
        if (definition.Actors.Count(a => a.Kind == "player") != 1) throw new ArgumentException("Exactly one avatar required.");
        Definition = definition;
        var cells = new List<Hex>();
        for (var q = 0; q <= definition.Width * 2; q++)
        for (var r = -definition.Width; r <= definition.Height * 2; r++)
        {
            var h = new Hex(q, r);
            if (Inside(h.World)) cells.Add(h);
        }
        Cells = cells.ToArray();
        foreach (var actor in definition.Actors)
            if (!Walkable(new(actor.Q, actor.R))) throw new ArgumentException($"Blocked actor spawn: {actor.Id}");
    }

    public static FacilityMap Load(string json) => new(JsonSerializer.Deserialize<MapDefinition>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new ArgumentException("Empty map definition."));
    public bool Inside(Point2 p) => p.X > 0 && p.Y > 0 && p.X < Definition.Width && p.Y < Definition.Height;
    public bool Walkable(Hex h, IReadOnlySet<Hex>? ignoredBlockers = null) => Inside(h.World)
        && (!DynamicBlockers.Contains(h) || ignoredBlockers?.Contains(h) == true)
        && !Definition.Props.Any(p => p.Cell == h)
        && !SolidWalls.Any(w => Geometry.DistanceToSegment(h.World, w.Start, w.End) < 0.18);

    public bool CanTraverse(Hex a, Hex b, IReadOnlySet<Hex>? ignoredBlockers = null) => a.Distance(b) == 1 && Walkable(a, ignoredBlockers) && Walkable(b, ignoredBlockers)
        && !SolidWalls.Any(w => Geometry.Intersects(a.World, b.World, w.Start, w.End));

    public bool ClearSight(Point2 a, Point2 b) => !SolidWalls.Any(w => Geometry.Intersects(a, b, w.Start, w.End))
        && !Definition.Props.Any(p => p.Kind == "cabinet" && Geometry.DistanceToSegment(p.Cell.World, a, b) < 0.17);

    public IReadOnlyList<Hex> FindPath(Hex start, Hex end, IReadOnlySet<Hex>? occupied = null,
        IReadOnlySet<Hex>? ignoredBlockers = null, bool approachBlockedGoal = false)
    {
        if (!Inside(end.World) || !approachBlockedGoal && (!Walkable(end, ignoredBlockers) || occupied?.Contains(end) == true)) return [];
        var queue = new Queue<Hex>();
        var previous = new Dictionary<Hex, Hex> { [start] = start };
        var closest = start;
        queue.Enqueue(start);
        while (queue.TryDequeue(out var current))
        {
            if (current.Distance(end) < closest.Distance(end)) closest = current;
            if (current == end)
                return Reconstruct(current);
            for (var facing = 0; facing < 6; facing++)
            {
                var next = current.Neighbor(facing);
                if (previous.ContainsKey(next) || !CanTraverse(current, next, ignoredBlockers) || occupied?.Contains(next) == true) continue;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }
        return approachBlockedGoal ? Reconstruct(closest) : [];

        IReadOnlyList<Hex> Reconstruct(Hex current)
        {
            var path = new List<Hex>();
            while (current != start) { path.Add(current); current = previous[current]; }
            path.Reverse();
            return path;
        }
    }
}
