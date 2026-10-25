using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Textured floor meshes with shared spatial shading samples, independent of sprite anchors.</summary>
public partial class GroundView : Node2D
{
    private const int Subdivisions = 8;
    public SpatialSimulation Simulation { get; set; } = null!;
    public IReadOnlyDictionary<string, Texture2D> Textures { get; set; } = null!;
    public IReadOnlyDictionary<string, Texture2D> AuthoredTextures { get; set; } = new Dictionary<string, Texture2D>();
    private bool _authored = true;
    private readonly List<Batch> _batches = [];
    private SightStamp? _stamp;
    public int ShadingBuilds { get; private set; }
    private sealed record SightStamp(Point2 Position, Hex Cell, int Facing, bool Attending, bool EyesOpen,
        bool LightsOn, bool FieldView, bool Authored, string Doors);
    private sealed record Batch(ArrayMesh Mesh, string Key, Texture2D Texture, Vector2[] Vertices, Vector2[] UVs, Vector2[] PlanarUVs,
        int[] Indices, Point2[] Ground, Color[] Tints);

    public override void _Ready()
    {
        ShowBehindParent = true;
        TextureFilter = TextureFilterEnum.Nearest;
        var map = Simulation.Map.Definition;
        var tiles = from y in Enumerable.Range(-2, map.Height + 4)
                    from x in Enumerable.Range(-2, map.Width + 4)
                    let indoor = x >= 0 && y >= 0 && x < map.Width && y < map.Height
                    let key = $"{(indoor ? "floor" : "land")}-{Math.Abs((x * 17 + y * 31) % 6)}"
                    select (x, y, indoor, key);
        foreach (var group in tiles.GroupBy(t => t.key))
        {
            var vertices = new List<Vector2>(); var uvs = new List<Vector2>(); var indices = new List<int>();
            var planar = new List<Vector2>();
            var ground = new List<Point2>(); var tints = new List<Color>();
            var size = Textures[group.Key].GetSize();
            foreach (var tile in group)
            {
                var first = vertices.Count;
                var tint = tile.indoor && tile.y is >= 3 and < 5 ? new Color(1.09f, 1.03f, 0.86f) : Colors.White;
                for (var v = 0; v <= Subdivisions; v++)
                for (var u = 0; u <= Subdivisions; u++)
                {
                    var offset = new Point2(u / (double)Subdivisions, v / (double)Subdivisions);
                    var point = new Point2(tile.x + offset.X, tile.y + offset.Y);
                    var uv = WorldView.Screen(offset) + new Vector2(0, 12);
                    vertices.Add(WorldView.Screen(point)); uvs.Add(uv / size);
                    planar.Add(new((float)offset.X, (float)offset.Y));
                    ground.Add(point); tints.Add(tint);
                }
                for (var v = 0; v < Subdivisions; v++)
                for (var u = 0; u < Subdivisions; u++)
                {
                    var a = first + v * (Subdivisions + 1) + u;
                    var b = a + 1; var c = a + Subdivisions + 1; var d = c + 1;
                    indices.AddRange([a, b, c, b, d, c]);
                }
            }
            _batches.Add(new(new ArrayMesh(), group.Key, Textures[group.Key], vertices.ToArray(), uvs.ToArray(), planar.ToArray(),
                indices.ToArray(), ground.ToArray(), tints.ToArray()));
        }
    }

    public void Refresh(bool fieldView, bool authored = true)
    {
        _authored = authored;
        var player = Simulation.Player;
        var stamp = new SightStamp(player.Position, player.Cell, player.Facing, player.Attending,
            Simulation.LocalMs >= player.EyesClosedUntilMs, Simulation.Map.LightsOn, fieldView, authored,
            string.Join(',', Simulation.Map.OpenDoors.Order(StringComparer.Ordinal)));
        if (stamp == _stamp) return;
        _stamp = stamp;
        var samples = new Dictionary<Point2, Color>();
        var dim = new Color(0.28f, 0.32f, 0.34f); var lit = new Color(0.92f, 1, 0.96f);
        foreach (var batch in _batches)
        {
            var colors = new Color[batch.Ground.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                var point = batch.Ground[i];
                if (!samples.TryGetValue(point, out var light))
                {
                    light = fieldView ? dim.Lerp(lit, FieldPresentation.SightShade(Simulation, point))
                        : stamp.LightsOn ? Colors.White : new Color(0.3f, 0.34f, 0.4f);
                    samples.Add(point, light);
                }
                colors[i] = light * batch.Tints[i];
            }
            using var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = batch.Vertices;
            arrays[(int)Mesh.ArrayType.TexUV] = authored && AuthoredTextures.ContainsKey(batch.Key) ? batch.PlanarUVs : batch.UVs;
            arrays[(int)Mesh.ArrayType.Color] = colors;
            arrays[(int)Mesh.ArrayType.Index] = batch.Indices;
            batch.Mesh.ClearSurfaces(); batch.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        }
        ShadingBuilds++;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var batch in _batches) DrawMesh(batch.Mesh, _authored && AuthoredTextures.TryGetValue(batch.Key, out var art) ? art : batch.Texture);
    }

    public override void _ExitTree()
    {
        foreach (var batch in _batches) batch.Mesh.Dispose();
    }
}
