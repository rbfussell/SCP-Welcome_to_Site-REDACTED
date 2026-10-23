using System.Text.Json;

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
public sealed record ActorDefinition(string Id, string Name, string Kind, int Q, int R, int Facing);
public sealed record MapDefinition(int Version, int Width, int Height, WallDefinition[] Walls, PropDefinition[] Props, ActorDefinition[] Actors);

/// <summary>Authored slice geometry. Collision and sight never consult sprite pixels.</summary>
public sealed class FacilityMap
{
    public MapDefinition Definition { get; }
    public HashSet<string> OpenDoors { get; } = [];
    public Hex[] Cells { get; }
    public IEnumerable<WallDefinition> SolidWalls => Definition.Walls.Where(w => !w.Door || !OpenDoors.Contains(w.Id));

    public FacilityMap(MapDefinition definition)
    {
        if (definition.Version != 1 || definition.Width is < 1 or > 64 || definition.Height is < 1 or > 64)
            throw new ArgumentException("Unsupported map version or dimensions.");
        if (definition.Walls.Any(w => w.Axis is not ("u" or "v"))) throw new ArgumentException("Invalid wall axis.");
        var ids = definition.Walls.Select(w => w.Id).Concat(definition.Props.Select(p => p.Id)).Concat(definition.Actors.Select(a => a.Id)).ToArray();
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
    public bool Walkable(Hex h) => Inside(h.World)
        && !Definition.Props.Any(p => p.Cell == h)
        && !SolidWalls.Any(w => Geometry.DistanceToSegment(h.World, w.Start, w.End) < 0.18);

    public bool CanTraverse(Hex a, Hex b) => a.Distance(b) == 1 && Walkable(a) && Walkable(b)
        && !SolidWalls.Any(w => Geometry.Intersects(a.World, b.World, w.Start, w.End));

    public bool ClearSight(Point2 a, Point2 b) => !SolidWalls.Any(w => Geometry.Intersects(a, b, w.Start, w.End))
        && !Definition.Props.Any(p => p.Kind == "cabinet" && Geometry.DistanceToSegment(p.Cell.World, a, b) < 0.17);

    public IReadOnlyList<Hex> FindPath(Hex start, Hex end, IReadOnlySet<Hex>? occupied = null)
    {
        if (!Walkable(end) || occupied?.Contains(end) == true) return [];
        var queue = new Queue<Hex>();
        var previous = new Dictionary<Hex, Hex> { [start] = start };
        queue.Enqueue(start);
        while (queue.TryDequeue(out var current))
        {
            if (current == end)
            {
                var path = new List<Hex>();
                while (current != start) { path.Add(current); current = previous[current]; }
                path.Reverse();
                return path;
            }
            for (var facing = 0; facing < 6; facing++)
            {
                var next = current.Neighbor(facing);
                if (previous.ContainsKey(next) || !CanTraverse(current, next) || occupied?.Contains(next) == true) continue;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }
        return [];
    }
}
