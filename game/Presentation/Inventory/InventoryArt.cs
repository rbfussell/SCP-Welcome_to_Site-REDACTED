using Godot;
using System.Text.Json;

namespace SitePresentation;

/// <summary>Object sprites and their shared source-image projection. Pixel size never defines physical capacity.</summary>
internal static class InventoryArt
{
    private static readonly Dictionary<string, AtlasTexture> Textures = [];
    private static readonly Dictionary<string, Image> Images = [];
    private sealed record MountIllustration(float Height, float[] Anchor, float RotationDegrees, float[]? ProviderOrigin = null);
    private static readonly Dictionary<string, MountIllustration> MountIllustrations = [];
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
        foreach (var binding in json.RootElement.GetProperty("attachmentIllustrations").EnumerateObject())
        {
            var profile = binding.Value.Deserialize<MountIllustration>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            if (profile is null || !Textures.ContainsKey(binding.Name) || !float.IsFinite(profile.Height) || profile.Height is <= 0 or > 256
                || profile.Anchor is not { Length: 2 } || profile.Anchor.Any(v => !float.IsFinite(v) || v is < 0 or > 1)
                || profile.ProviderOrigin is { } origin && (origin.Length != 2 || origin.Any(v => !float.IsFinite(v) || v is < 0 or > 1))
                || !float.IsFinite(profile.RotationDegrees) || Math.Abs(profile.RotationDegrees) > 360)
                throw new InvalidOperationException("Invalid mounted illustration: " + binding.Name);
            MountIllustrations.Add(binding.Name, profile);
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
        Textures.Clear(); MountIllustrations.Clear(); _loaded = false;
    }
    public static void Draw(Control canvas, string definitionId, Rect2 bounds, bool rotated, float opacity = 1)
    {
        if (GetLayout(definitionId, bounds, rotated) is not { } layout) return;
        canvas.DrawSetTransform(layout.Center, layout.Turn ? Mathf.Pi / 2 : 0);
        canvas.DrawTextureRect(layout.Texture, new(-layout.Size / 2, layout.Size), false, new(1, 1, 1, opacity));
        canvas.DrawSetTransform(Vector2.Zero);
    }
    private readonly record struct MountLayout(AtlasTexture Texture, Vector2 Size, Vector2 Anchor, float Angle)
    {
        public Vector2 Project(Vector2 source) => ((source - Anchor) * Size).Rotated(Angle);
        public Vector2 Unproject(Vector2 point) => point.Rotated(-Angle) / Size + Anchor;
    }
    private static MountLayout MountedLayout(string id, int rotation)
    {
        Load(); var profile = MountIllustrations[id]; var texture = Textures[id];
        return new(texture, texture.GetSize() * (profile.Height / texture.GetHeight()), new(profile.Anchor[0], profile.Anchor[1]),
            Mathf.DegToRad(profile.RotationDegrees + rotation * 60));
    }
    internal static Vector2 ProjectMount(string id, int rotation, Vector2 source) => MountedLayout(id, rotation).Project(source);
    internal static Vector2 UnprojectMount(string id, int rotation, Vector2 point) => MountedLayout(id, rotation).Unproject(point);
    internal static Vector2 ProviderOrigin(string id, Vector2 size)
    {
        Load(); return MountIllustrations.TryGetValue(id, out var profile) && profile.ProviderOrigin is { } p
            ? new Vector2(p[0], p[1]) * size : size / 2;
    }
    internal static bool HasMountIllustration(string id) { Load(); return MountIllustrations.ContainsKey(id); }
    internal static void DrawMount(Control canvas, string id, Vector2 origin, int rotation, float opacity, bool highlight = false)
    {
        if (!HasMountIllustration(id)) return;
        var layout = MountedLayout(id, rotation);
        canvas.DrawSetTransform(origin, layout.Angle);
        canvas.DrawTextureRect(layout.Texture, new(-layout.Anchor * layout.Size, layout.Size), false,
            highlight ? new(1.3f, 1.5f, 1.25f, opacity) : new(1, 1, 1, opacity));
        canvas.DrawSetTransform(Vector2.Zero);
    }
    internal static void Highlight(Control canvas, string id, Rect2 bounds, Vector2[] polygon, bool blocked = false)
    {
        if (GetLayout(id, bounds) is not { } layout) return;
        var points = polygon.Select(layout.Project).ToArray();
        var texture = layout.Texture;
        var uv = polygon.Select(p => (texture.Region.Position + p * texture.Region.Size) / texture.Atlas.GetSize()).ToArray();
        // Re-draw only this sprite segment: its alpha and material texture remain intact.
        canvas.DrawPolygon(points, new[] { blocked ? new Color(1.8f, 1.0f, 0.85f, 0.65f) : new Color(1.45f, 1.65f, 1.35f, 0.65f) }, uv, texture.Atlas);
        canvas.DrawPolyline(points.Append(points[0]).ToArray(), new Color(blocked ? "ff8c78" : "b5d2a1", 0.75f), 1, false);
    }
}
