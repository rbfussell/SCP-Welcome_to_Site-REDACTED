using System.Text.Json;

namespace SiteSimulation;

public sealed class ActorState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public Hex Cell { get; set; }
    public List<Hex> Route { get; set; } = [];
    public int SegmentMs { get; set; }
    public int Facing { get; set; }
    public bool KeepFacing { get; set; }
    public long WalkMs { get; set; }
    public Point2 Position => Route.Count == 0 ? Cell.World : Cell.World + (Route[0].World - Cell.World) * (SegmentMs / (double)SpatialSimulation.StepDurationMs);
    public bool Moving => Route.Count > 0;
}

public sealed record SpatialSnapshot(int Version, string MapIdentity, long WorldMs, long LocalMs, bool LocalHold, bool ManualPause,
    int Speed, string[] OpenDoors, ActorState[] Actors, int PatrolIndex);

/// <summary>Fixed-step prototype. No Godot dependency, wall clock or animation callbacks.</summary>
public sealed class SpatialSimulation
{
    public const int TickMs = 50;
    public const int StepDurationMs = 300;
    public FacilityMap Map { get; }
    public List<ActorState> Actors { get; private set; }
    public ActorState Player => Actors.Single(a => a.Kind == "player");
    public long WorldMs { get; private set; }
    public long LocalMs { get; private set; }
    public bool LocalHold { get; set; }
    public bool ManualPause { get; set; }
    public int Speed { get; set; } = 1;
    public int OffsiteUpdates => (int)(WorldMs / 1000);
    public int LocalCountdownMs => 10000 - (int)(LocalMs % 10000);
    public string LastMessage { get; private set; } = "Click the corridor floor to move. White = blocked geometry.";
    private readonly string _mapIdentity;
    private int _patrolIndex;
    private static readonly Hex[] Patrol = [new(5, 11), new(13, 7), new(13, 5), new(5, 9)];

    public SpatialSimulation(string json)
    {
        _mapIdentity = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
        Map = FacilityMap.Load(json);
        Actors = Map.Definition.Actors.Select(a => new ActorState { Id = a.Id, Name = a.Name, Kind = a.Kind, Cell = new(a.Q, a.R), Facing = a.Facing }).ToList();
    }

    private HashSet<Hex> Occupied(ActorState except) => Actors.Where(a => a != except).SelectMany(a => a.Moving ? new[] { a.Cell, a.Route[0] } : new[] { a.Cell }).ToHashSet();

    public bool RequestMove(Hex destination, bool keepFacing = false)
    {
        if (ManualPause || LocalHold) { LastMessage = "Local activity is held. Resume before moving."; return false; }
        if (Player.Moving) { LastMessage = "Finish this route first (Esc cancels at the next hex)."; return false; }
        var path = Map.FindPath(Player.Cell, destination, Occupied(Player));
        if (path.Count == 0) { LastMessage = "No clear route. Walls, closed doors and occupied hexes block movement."; return false; }
        Player.Route = path.ToList();
        Player.KeepFacing = keepFacing;
        LastMessage = $"Route committed: {path.Count} hexes. {(keepFacing ? "Maintaining attention." : "Facing follows movement.")}";
        return true;
    }

    public void CancelMove()
    {
        if (ManualPause || LocalHold) { LastMessage = "Cannot change a held local action."; return; }
        Player.Route = Player.Route.Take(Player.SegmentMs > 0 ? 1 : 0).ToList();
        LastMessage = "Route cancelled at the next safe hex boundary.";
    }

    public void Face(Point2 target)
    {
        if (ManualPause || LocalHold) { LastMessage = "Attention is held with the rest of the local simulation."; return; }
        var delta = target - Player.Position;
        if (delta.Length < 0.001) return;
        // Compare in unprojected hex coordinates, independent of camera or sprite.
        var dq = delta.X * 2;
        var dr = delta.Y * 2 - delta.X;
        var x = Math.Sqrt(3) * (dq + dr / 2);
        var y = 1.5 * dr;
        var best = double.NegativeInfinity;
        for (var f = 0; f < 6; f++)
        {
            var d = Hex.Directions[f];
            var dx = Math.Sqrt(3) * (d.Q + d.R / 2.0);
            var dy = 1.5 * d.R;
            var dot = x * dx + y * dy;
            if (dot > best) { best = dot; Player.Facing = f; }
        }
        Player.KeepFacing = true;
        LastMessage = "Attention changed independently of movement.";
    }

    public bool ToggleNearestDoor()
    {
        if (ManualPause || LocalHold) { LastMessage = "Door use is held with local actions."; return false; }
        var door = Map.Definition.Walls.Where(w => w.Door).OrderBy(w => Point2.Distance(Player.Position, w.Center)).First();
        if (Point2.Distance(Player.Position, door.Center) > 1.35) { LastMessage = "Approach a doorway, then press E."; return false; }
        if (Map.OpenDoors.Contains(door.Id))
        {
            if (Actors.Any(a => Geometry.DistanceToSegment(a.Position, door.Start, door.End) < 0.25
                || a.Moving && Geometry.Intersects(a.Position, a.Route[0].World, door.Start, door.End)))
            { LastMessage = "Door obstructed by an actor or a committed crossing."; return false; }
            Map.OpenDoors.Remove(door.Id);
        }
        else Map.OpenDoors.Add(door.Id);
        LastMessage = $"{door.Id}: {(Map.OpenDoors.Contains(door.Id) ? "open" : "closed")}. Collision and sight updated together.";
        return true;
    }

    public bool VisibleToPlayer(Point2 target)
    {
        var p = Player.Position;
        var d = target - p;
        if (d.Length < 0.05) return true;
        var direction = Player.Cell.Neighbor(Player.Facing).World - Player.Cell.World;
        // Directional cone in the ground plane; no sprite/pixel test.
        var dot = (d.X * direction.X + d.Y * direction.Y) / (d.Length * direction.Length);
        return d.Length < 5 && dot >= 0.45 && Map.ClearSight(p, target);
    }

    public void Tick()
    {
        if (ManualPause) return;
        WorldMs += TickMs;
        if (LocalHold) return;
        LocalMs += TickMs;
        foreach (var actor in Actors)
        {
            if (!actor.Moving) continue;
            if (!Map.CanTraverse(actor.Cell, actor.Route[0]) || Occupied(actor).Contains(actor.Route[0]))
            {
                actor.Route.Clear(); actor.SegmentMs = 0;
                if (actor == Player) LastMessage = "Route stopped: geometry or occupancy changed.";
                continue;
            }
            if (!actor.KeepFacing) actor.Facing = Array.FindIndex(Hex.Directions, d => d.Q == actor.Route[0].Q - actor.Cell.Q && d.R == actor.Route[0].R - actor.Cell.R);
            actor.SegmentMs += TickMs;
            actor.WalkMs += TickMs;
            if (actor.SegmentMs < StepDurationMs) continue;
            actor.Cell = actor.Route[0];
            actor.Route.RemoveAt(0);
            actor.SegmentMs = 0;
        }
        var patrol = Actors.Single(a => a.Id == "tech");
        if (!patrol.Moving && LocalMs % 1000 == 0)
        {
            var target = Patrol[_patrolIndex++ % Patrol.Length];
            patrol.Route = Map.FindPath(patrol.Cell, target, Occupied(patrol)).ToList();
        }
    }

    public string Save() => JsonSerializer.Serialize(new SpatialSnapshot(1, _mapIdentity, WorldMs, LocalMs, LocalHold, ManualPause,
        Speed, Map.OpenDoors.Order().ToArray(), Actors.ToArray(), _patrolIndex));

    public void Restore(string json)
    {
        var snapshot = JsonSerializer.Deserialize<SpatialSnapshot>(json) ?? throw new ArgumentException("Empty save.");
        if (snapshot.Version != 1 || snapshot.MapIdentity != _mapIdentity || snapshot.WorldMs < 0 || snapshot.LocalMs < 0
            || snapshot.WorldMs % TickMs != 0 || snapshot.LocalMs % TickMs != 0 || snapshot.PatrolIndex < 0
            || snapshot.Actors is null || snapshot.OpenDoors is null || snapshot.Actors.Any(a => a is null)
            || snapshot.LocalMs > snapshot.WorldMs || snapshot.Speed is not (1 or 2 or 4)
            || !snapshot.Actors.Select(a => a.Id).Order().SequenceEqual(Actors.Select(a => a.Id).Order())
            || snapshot.OpenDoors.Any(id => !Map.Definition.Walls.Any(w => w.Id == id && w.Door))
            || snapshot.Actors.Any(a => a is null || a.Route is null || a.WalkMs < 0 || a.SegmentMs < 0 || a.SegmentMs >= StepDurationMs
                || a.SegmentMs % TickMs != 0 || a.Facing is < 0 or > 5
                || !Map.Definition.Actors.Any(d => d.Id == a.Id && d.Kind == a.Kind && d.Name == a.Name)))
            throw new ArgumentException("Incompatible or invalid prototype save.");
        var candidate = new FacilityMap(Map.Definition);
        candidate.OpenDoors.UnionWith(snapshot.OpenDoors);
        foreach (var actor in snapshot.Actors)
        {
            if (!candidate.Walkable(actor.Cell) || actor.Route.Count == 0 && actor.SegmentMs != 0)
                throw new ArgumentException("Save contains invalid actor occupancy/progress.");
            if (actor.SegmentMs > 0 && !candidate.CanTraverse(actor.Cell, actor.Route[0]))
                throw new ArgumentException("Saved in-progress movement crosses blocked geometry.");
            var previous = actor.Cell;
            foreach (var next in actor.Route)
            {
                if (previous.Distance(next) != 1 || !candidate.Inside(next.World)) throw new ArgumentException("Saved route has an invalid step.");
                previous = next;
            }
        }
        if (snapshot.Actors.Select(a => a.Cell).Distinct().Count() != snapshot.Actors.Length)
            throw new ArgumentException("Save overlaps physical actors.");
        Map.OpenDoors.Clear();
        Map.OpenDoors.UnionWith(snapshot.OpenDoors);
        Actors = snapshot.Actors.ToList();
        WorldMs = snapshot.WorldMs; LocalMs = snapshot.LocalMs;
        LocalHold = snapshot.LocalHold; ManualPause = snapshot.ManualPause; Speed = snapshot.Speed;
        _patrolIndex = snapshot.PatrolIndex;
        LastMessage = "Saved progress restored, including mid-step position and held timers.";
    }
}
