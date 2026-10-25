using Godot;
using SiteSimulation;

namespace SitePresentation;

public partial class WorldView
{
    private sealed record CutawayMesh(ArrayMesh Mesh, Vector2 Center, double Radius, Color Light, Vector2[] Corners);
    private readonly Dictionary<string, CutawayMesh> _cutawayMeshes = [];
    private readonly Dictionary<(string Actor, bool Diagnostic), float> _cutawayHeights = [];
    private PlayerWallCutaway _playerCutaway;
    private float _playerDepth;
    // A small grid gives a smooth radial alpha transition across short wall faces, including their caps.
    private const float CutawayVertexSpacing = 4;
    internal Vector2 CutawayCenter => new((float)_playerCutaway.Center.X, (float)_playerCutaway.Center.Y);
    internal float CutawayRadius => (float)_playerCutaway.Radius;
    internal int FeatheredWallFaces { get; private set; }

    private void RefreshCutaway()
    {
        var feet = Screen(Simulation.Player.Position);
        var actor = Simulation.Player;
        if (!_cutawayHeights.TryGetValue((actor.Id, DiagnosticSprites), out var height))
        {
            // Keep a stable body envelope over every facing/step, so the feather does not pulse while walking.
            var keys = !DiagnosticSprites && _facilityArt.Manifest.Actors.TryGetValue(actor.Id, out var appearance)
                ? appearance.Layers.SelectMany(layer => layer.States.Values).SelectMany(directions => directions).SelectMany(frames => frames)
                : _sprites.Keys.Where(key => key.StartsWith(actor.Kind + "-", StringComparison.Ordinal));
            height = keys.Distinct().Max(key => _sprites[key].Texture.GetHeight() * _sprites[key].Scale);
            _cutawayHeights.Add((actor.Id, DiagnosticSprites), height);
        }
        _playerCutaway = PlayerWallCutaway.AroundSprite(new(feet.X, feet.Y), height);
        _playerDepth = feet.Y;
        FeatheredWallFaces = 0;
    }

    private bool UsesPlayerCutaway(WallDefinition wall)
    {
        // Doors share map geometry with walls, but keep their normal opacity in both open and closed states.
        if (wall.Door || !Cutaway || Screen(wall.Center).Y < _playerDepth) return false;
        var start = Screen(wall.Start); var end = Screen(wall.End);
        return _playerCutaway.Intersects(Math.Min(start.X, end.X) - 4, Math.Min(start.Y, end.Y) - WallHeight - 4,
            Math.Max(start.X, end.X) + 4, Math.Max(start.Y, end.Y) + 4);
    }

    private float CutawayAlpha(Vector2 point) => (float)_playerCutaway.Opacity(new(point.X, point.Y));

    /// <summary>Draw a textured quad with continuous vertex alpha; reuse its mesh until the mask or lighting changes.</summary>
    private void FeatheredQuad(string id, Vector2[] corners, Color light, Texture2D? texture = null)
    {
        if (!_cutawayMeshes.TryGetValue(id, out var cached) || cached.Center != CutawayCenter
            || cached.Radius != _playerCutaway.Radius || cached.Light != light || !cached.Corners.SequenceEqual(corners))
        {
            var mesh = cached?.Mesh ?? new ArrayMesh();
            mesh.ClearSurfaces();
            // Order: bottom-left, bottom-right, top-right, top-left, matching the normal wall UVs.
            var columns = Math.Max(1, (int)Math.Ceiling(corners[0].DistanceTo(corners[1]) / CutawayVertexSpacing));
            var rows = Math.Max(1, (int)Math.Ceiling(corners[0].DistanceTo(corners[3]) / CutawayVertexSpacing));
            var vertices = new Vector2[(columns + 1) * (rows + 1)];
            var colors = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (var y = 0; y <= rows; y++)
            for (var x = 0; x <= columns; x++)
            {
                var u = x / (float)columns; var v = y / (float)rows;
                var point = corners[0].Lerp(corners[1], u).Lerp(corners[3].Lerp(corners[2], u), v);
                var index = y * (columns + 1) + x;
                vertices[index] = point;
                colors[index] = new Color(light, light.A * CutawayAlpha(point));
                uvs[index] = new(u, 1 - v);
            }
            var indices = new int[columns * rows * 6]; var next = 0;
            for (var y = 0; y < rows; y++)
            for (var x = 0; x < columns; x++)
            {
                var a = y * (columns + 1) + x; var b = a + 1; var d = a + columns + 1; var c = d + 1;
                indices[next++] = a; indices[next++] = b; indices[next++] = c;
                indices[next++] = a; indices[next++] = c; indices[next++] = d;
            }
            using var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = vertices;
            arrays[(int)Mesh.ArrayType.Color] = colors;
            arrays[(int)Mesh.ArrayType.TexUV] = uvs;
            arrays[(int)Mesh.ArrayType.Index] = indices;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, flags: Mesh.ArrayFormat.FlagUse2DVertices);
            _cutawayMeshes[id] = new(mesh, CutawayCenter, _playerCutaway.Radius, light, corners);
        }
        DrawMesh(_cutawayMeshes[id].Mesh, texture, Transform2D.Identity);
    }

    private void WallStroke(Vector2 start, Vector2 end, Color color, float width, bool feather)
    {
        if (!feather) { DrawLine(start, end, color, width); return; }
        var segments = Math.Max(1, (int)Math.Ceiling(start.DistanceTo(end) / 2));
        for (var i = 0; i < segments; i++)
        {
            var a = start.Lerp(end, i / (float)segments); var b = start.Lerp(end, (i + 1) / (float)segments);
            DrawLine(a, b, new Color(color, color.A * CutawayAlpha((a + b) * 0.5f)), width);
        }
    }

    private void DiagnosticWall(WallDefinition wall, bool feather)
    {
        var sprite = _sprites[$"{(wall.Door ? "door" : "wall")}-{wall.Axis}"];
        var feet = Screen(wall.Start);
        if (!feather) { Sprite($"{(wall.Door ? "door" : "wall")}-{wall.Axis}", feet); return; }
        var rect = new Rect2((feet - sprite.Anchor * sprite.Scale).Round(), sprite.Texture.GetSize() * sprite.Scale);
        FeatheredQuad(wall.Id + ":diagnostic", [new(rect.Position.X, rect.End.Y), rect.End, new(rect.End.X, rect.Position.Y), rect.Position],
            LightAt(feet), sprite.Texture);
    }

    public override void _ExitTree()
    {
        foreach (var entry in _cutawayMeshes.Values) entry.Mesh.Dispose();
        _cutawayMeshes.Clear();
    }
}
