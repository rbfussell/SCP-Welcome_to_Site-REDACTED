using Godot;
using SiteSimulation;
using System.Text.Json;

namespace SitePresentation;

/// <summary>Original stand-in artwork over reference-scale floor/hex projection.</summary>
public partial class WorldView : Node2D
{
    public SpatialSimulation Simulation { get; set; } = null!;
    public bool CollisionOverlay { get; set; } = true;
    public bool HexGrid { get; set; }
    public bool FloorGrid { get; set; }
    public bool SightOverlay { get; set; } = true;
    public bool Cutaway { get; set; } = true;
    public Hex? Hover { get; set; }
    public IReadOnlyList<Hex> Preview { get; set; } = [];
    private readonly Dictionary<string, (Texture2D Texture, Vector2 Anchor)> _sprites = [];
    private readonly List<Rect2> _debugLabels = [];
    private readonly Dictionary<string, string[]> _anomalySprites = [];
    private int _walkFrames = 3;
    private int _walkCycleMs = 300;
    private int Frame(ActorState actor) => actor.Moving ? (int)(actor.WalkMs % _walkCycleMs * _walkFrames / _walkCycleMs) : 0;
    private static readonly Color Mint = new("9ed4b0");
    private static readonly Color White = new("f2f0dc");
    private static readonly Color Amber = new("d5ad67");
    private static readonly string[] FacingNames = ["NE", "E", "SE", "SW", "W", "NW"];
    public static Vector2 Screen(Point2 p)
    {
        var s = ReferenceProjection.Project(p);
        return new((float)s.X, (float)s.Y);
    }

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        using var manifest = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Assets/spatial-manifest.json"));
        foreach (var binding in manifest.RootElement.GetProperty("anomaly_sprites").EnumerateObject())
            _anomalySprites[binding.Name] = binding.Value.EnumerateArray().Select(x => x.GetString()!).ToArray();
        _walkFrames = manifest.RootElement.GetProperty("animation").GetProperty("walk_frames").GetInt32();
        _walkCycleMs = manifest.RootElement.GetProperty("animation").GetProperty("walk_cycle_ms").GetInt32();
        foreach (var entry in manifest.RootElement.GetProperty("sprites").EnumerateObject())
        {
            var anchor = entry.Value.GetProperty("anchor");
            _sprites[entry.Name] = (GD.Load<Texture2D>("res://Assets/" + entry.Value.GetProperty("file").GetString()), new(anchor[0].GetSingle(), anchor[1].GetSingle()));
        }
    }

    private void Sprite(string key, Vector2 feet, Color? tint = null)
    {
        var sprite = _sprites[key];
        DrawTexture(sprite.Texture, (feet - sprite.Anchor).Round(), (tint ?? Colors.White) * (Simulation.Map.LightsOn ? Colors.White : new Color(0.3f, 0.34f, 0.4f)));
    }

    private static Vector2[] Corners(Hex h)
    {
        var center = Screen(h.World);
        return ReferenceProjection.HexCorners.Select(p => center + new Vector2((float)p.X, (float)p.Y)).ToArray();
    }

    private void Outline(Vector2[] points, Color color, float width = 1)
    {
        DrawPolyline(points.Append(points[0]).ToArray(), color, width, false);
    }

    private void Tag(Vector2 p, string text, Color color, int size = 9)
    {
        var font = ThemeDB.FallbackFont;
        var width = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        var original = p;
        var rect = new Rect2(p.X - width / 2 - 3, p.Y - size, width + 6, size + 5);
        for (var attempt = 0; attempt < 10 && _debugLabels.Any(r => r.Intersects(rect)); attempt++)
        { p.Y -= size + 8; rect.Position = new(p.X - width / 2 - 3, p.Y - size); }
        _debugLabels.Add(rect);
        if (p != original) DrawLine(original, p, new Color(White, 0.45f), 0.8f);
        DrawRect(rect, new Color(0.04f, 0.06f, 0.06f, 0.9f));
        DrawString(font, new Vector2(p.X - width / 2, p.Y), text, HorizontalAlignment.Left, -1, size, color);
    }

    public override void _Draw()
    {
        if (Simulation is null) return;
        _debugLabels.Clear();
        var map = Simulation.Map;
        for (var y = -2; y < map.Definition.Height + 2; y++)
        for (var x = -2; x < map.Definition.Width + 2; x++)
        {
            var indoor = x >= 0 && y >= 0 && x < map.Definition.Width && y < map.Definition.Height;
            var variant = Math.Abs((x * 17 + y * 31) % 6);
            var tint = indoor && y is >= 3 and < 5 ? new Color(1.09f, 1.03f, 0.86f) : Colors.White;
            Sprite($"{(indoor ? "floor" : "land")}-{variant}", Screen(new(x, y)), tint);
            if (indoor && FloorGrid)
                Outline([Screen(new(x, y)), Screen(new(x + 1, y)), Screen(new(x + 1, y + 1)), Screen(new(x, y + 1))], new Color(Amber, 0.75f));
        }

        foreach (var wall in map.SolidWalls)
        {
            var a = Screen(wall.Start); var b = Screen(wall.End);
            DrawColoredPolygon([a, b, b + new Vector2(9, 8), a + new Vector2(9, 8)], new Color(0.01f, 0.02f, 0.02f, 0.2f));
        }
        foreach (var h in map.Cells)
        {
            var corners = Corners(h);
            if (SightOverlay && Simulation.VisibleToPlayer(h.World) && map.Walkable(h))
                DrawColoredPolygon(corners, new Color(Mint, 0.09f));
            if (HexGrid) Outline(corners, new Color(White, 0.13f));
        }
        DrawString(ThemeDB.FallbackFont, Screen(new(0.9, 1.2)), "OBSERVATION / LAB", HorizontalAlignment.Left, -1, 11, new Color(White, 0.46f));
        DrawString(ThemeDB.FallbackFont, Screen(new(0.8, 3.7)), "SERVICE CORRIDOR", HorizontalAlignment.Left, -1, 9, new Color(White, 0.45f));
        DrawString(ThemeDB.FallbackFont, Screen(new(0.7, 6.4)), "OPERATIONS / STORES", HorizontalAlignment.Left, -1, 11, new Color(White, 0.46f));

        foreach (var door in map.Definition.Walls.Where(w => w.Door))
        {
            DrawLine(Screen(door.Start), Screen(door.End), new Color(Amber, 0.8f), 3);
            var c = Screen(door.Center);
            DrawCircle(c + new Vector2(0, -2), 2, map.OpenDoors.Contains(door.Id) ? Mint : Amber);
        }
        var committed = new[] { Simulation.Player.Cell }.Concat(Simulation.Player.Route).Select(h => Screen(h.World)).ToArray();
        if (committed.Length > 1) DrawPolyline(committed, Mint, 1.4f, false);
        if (Preview.Count > 0 && !Simulation.Player.Moving)
        {
            foreach (var cell in Preview) DrawCircle(Screen(cell.World), 1.5f, new Color(Mint, 0.7f));
        }
        DrawArc(Screen(Simulation.Player.Position), 9, 0, Mathf.Tau, 24, Mint, 1);

        // Short wall segments and object feet participate in the same depth order.
        var drawables = new List<(float Depth, string Id, Action Draw)>();
        foreach (var wall in map.Definition.Walls)
        {
            var c = Screen(wall.Center);
            drawables.Add((c.Y, wall.Id, () =>
            {
                var open = wall.Door && map.OpenDoors.Contains(wall.Id);
                if (open)
                {
                    DrawLine(Screen(wall.Start), Screen(wall.Start) - new Vector2(0, 54), new Color("b4aaa0"), 3);
                    DrawLine(Screen(wall.End), Screen(wall.End) - new Vector2(0, 54), new Color("b4aaa0"), 3);
                    DrawLine(Screen(wall.Start) - new Vector2(0, 54), Screen(wall.End) - new Vector2(0, 54), new Color("888681"), 4);
                    return;
                }
                var nearWall = wall.Y >= 5 || wall.X == map.Definition.Width;
                Sprite($"{(wall.Door ? "door" : "wall")}-{wall.Axis}", Screen(wall.Start), new Color(1, 1, 1, Cutaway && nearWall ? 0.25f : 1));
            }));
        }
        foreach (var prop in map.Definition.Props)
        {
            var p = Screen(prop.Cell.World);
            drawables.Add((p.Y, prop.Id, () => Sprite(prop.Kind, p)));
        }
        foreach (var body in Simulation.Anomalies.Bodies)
        {
            var p = Screen(body.Cell.World);
            drawables.Add((p.Y, body.Id, () => Sprite(_anomalySprites[body.ModuleId][body.Pose], p)));
        }
        foreach (var actor in Simulation.Actors)
        {
            var p = Screen(actor.Position);
            drawables.Add((p.Y, actor.Id, () =>
            {
                var state = actor.Moving ? "walk" : "idle";
                var frame = Frame(actor);
                Sprite($"{actor.Kind}-{state}-{actor.Facing}-{frame}", p);
                var direction = Screen(actor.Cell.Neighbor(actor.Facing).World) - Screen(actor.Cell.World);
                DrawLine(p, p + direction.Normalized() * 15, new Color("61ff70"), 2);
            }));
        }
        foreach (var item in drawables.OrderBy(d => d.Depth).ThenBy(d => d.Id, StringComparer.Ordinal)) item.Draw();

        // White lines deliberately sit above the artwork: these are debug geometry, not an art mask.
        if (CollisionOverlay)
        {
            foreach (var h in map.Cells.Where(h => !map.Walkable(h)))
            {
                var corners = Corners(h);
                DrawColoredPolygon(corners, new Color(White, 0.05f));
                Outline(corners, new Color(White, 0.65f));
            }
            foreach (var wall in map.SolidWalls) DrawLine(Screen(wall.Start), Screen(wall.End), White, 1.3f, false);
        }
        foreach (var status in Simulation.InspectAnomalies())
        {
            var body = Simulation.Anomalies.Bodies.Single(b => b.Id == status.Id);
            var p = Screen(body.Cell.World);
            DrawArc(p, 10, 0, Mathf.Tau, 24, new Color("ceb4df"), 1.2f);
            foreach (var observer in status.Observation.EligibleIds)
                DrawLine(p, Screen(Simulation.Actors.Single(a => a.Id == observer).Position), new Color(Mint, 0.6f), 1, false);
            Tag(p + new Vector2(0, -88), $"SCP-650 · POSE {body.Pose + 1}/{body.PoseCount}", new Color("ceb4df"));
            Tag(p + new Vector2(0, 18), $"{status.Observation.EligibleIds.Length} eligible · {(status.Observation.Observed ? "FROZEN" : "UNOBSERVED")}", White, 8);
        }
        if (Hover is { } hover)
        {
            var good = map.Walkable(hover) && !Simulation.Actors.Any(a => a.Cell == hover);
            Outline(Corners(hover), good ? Mint : new Color("e6a180"), 1.7f);
        }
        foreach (var actor in Simulation.Actors)
        {
            var p = Screen(actor.Position);
            var state = !actor.Attending ? "DISTRACTED" : Simulation.LocalMs < actor.EyesClosedUntilMs ? "BLINK" : Simulation.LocalMs < actor.StartledUntilMs ? "STARTLED" : actor.Moving ? $"walk:{Frame(actor) + 1}/{_walkFrames}" : "idle:1/1";
            Tag(p + new Vector2(0, -65), $"{actor.Name}  {FacingNames[actor.Facing]}", actor.Kind == "player" ? new Color("efb4cb") : new Color("a5d8e2"));
            Tag(p + new Vector2(0, 16), state, White, 8);
        }
        foreach (var prop in map.Definition.Props)
            Tag(Screen(prop.Cell.World) + new Vector2(0, prop.Kind == "crate" ? -23 : -76), prop.Label, Amber, 8);
    }
}
