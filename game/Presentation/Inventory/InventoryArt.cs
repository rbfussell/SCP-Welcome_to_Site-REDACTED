using Godot;
using System.Text.Json;

namespace SitePresentation;

/// <summary>Object sprites and their shared source-image projection. Pixel size never defines physical capacity.</summary>
internal static class InventoryArt
{
    private static readonly Dictionary<string, AtlasTexture> Textures = [];
    private static readonly Dictionary<string, Image> Images = [];
    private static bool _loaded;
    private static void Load()
    {
        if (_loaded) return;
        using var json = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Assets/inventory-objects/manifest.json"));
        foreach (var asset in json.RootElement.GetProperty("assets").EnumerateArray())
        {
            var r = asset.GetProperty("region").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            Textures.Add(asset.GetProperty("id").GetString()!, new AtlasTexture
            { Atlas = GD.Load<Texture2D>("res://Assets/inventory-objects/" + asset.GetProperty("file").GetString()), Region = new(r[0], r[1], r[2], r[3]) });
        }
        _loaded = true;
    }
    private readonly record struct Layout(AtlasTexture Texture, Vector2 Center, Vector2 Size, bool Turn)
    {
        public Vector2 Project(Vector2 normalized)
        {
            var p = (normalized - Vector2.One * 0.5f) * Size;
            return Center + (Turn ? new Vector2(-p.Y, p.X) : p);
        }
        public Vector2 Unproject(Vector2 screen)
        {
            var p = screen - Center;
            return (Turn ? new Vector2(p.Y, -p.X) : p) / Size + Vector2.One * 0.5f;
        }
    }
    private static Layout? GetLayout(string id, Rect2 bounds, bool rotated = false)
    {
        Load();
        if (!Textures.TryGetValue(id, out var texture)) return null;
        var turn = rotated ^ (id == "gum-pack");
        var available = turn ? new Vector2(bounds.Size.Y, bounds.Size.X) : bounds.Size;
        var scale = Math.Min(available.X / texture.GetWidth(), available.Y / texture.GetHeight());
        return new(texture, bounds.GetCenter(), texture.GetSize() * scale, turn);
    }
    internal static Vector2 Project(string id, Rect2 bounds, Vector2 point) => GetLayout(id, bounds)!.Value.Project(point);
    internal static Vector2 Unproject(string id, Rect2 bounds, Vector2 point) => GetLayout(id, bounds)!.Value.Unproject(point);
    internal static bool Opaque(string id, Vector2 normalized)
    {
        if (normalized.X is < 0 or >= 1 || normalized.Y is < 0 or >= 1) return false;
        Load(); var texture = Textures[id];
        if (!Images.TryGetValue(id, out var image)) Images.Add(id, image = texture.Atlas.GetImage());
        var pixel = texture.Region.Position + normalized * texture.Region.Size;
        return image.GetPixel((int)pixel.X, (int)pixel.Y).A > 0.08f;
    }
    public static void Release()
    {
        foreach (var image in Images.Values) image.Dispose();
        Images.Clear();
        foreach (var texture in Textures.Values) { var source = texture.Atlas; texture.Dispose(); source?.Dispose(); }
        Textures.Clear(); _loaded = false;
    }
    public static void Draw(Control canvas, string definitionId, Rect2 bounds, bool rotated, float opacity = 1)
    {
        if (GetLayout(definitionId, bounds, rotated) is not { } layout) return;
        canvas.DrawSetTransform(layout.Center, layout.Turn ? Mathf.Pi / 2 : 0);
        canvas.DrawTextureRect(layout.Texture, new(-layout.Size / 2, layout.Size), false, new(1, 1, 1, opacity));
        canvas.DrawSetTransform(Vector2.Zero);
    }
    internal static void Highlight(Control canvas, string id, Rect2 bounds, Vector2[] polygon)
    {
        if (GetLayout(id, bounds) is not { } layout) return;
        var points = polygon.Select(layout.Project).ToArray();
        var texture = layout.Texture;
        var uv = polygon.Select(p => (texture.Region.Position + p * texture.Region.Size) / texture.Atlas.GetSize()).ToArray();
        // Re-draw only this sprite segment: its alpha and material texture remain intact.
        canvas.DrawPolygon(points, new[] { new Color(1.45f, 1.65f, 1.35f, 0.65f) }, uv, texture.Atlas);
        canvas.DrawPolyline(points.Append(points[0]).ToArray(), new Color("b5d2a1", 0.60f), 1, false);
    }
}
