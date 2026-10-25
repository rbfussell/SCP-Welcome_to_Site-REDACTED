using Godot;
using System.Text.Json;

namespace SitePresentation;

/// <summary>Generated object sprites, independent of physical item envelopes and placements.</summary>
internal static class InventoryArt
{
    private static readonly Dictionary<string, AtlasTexture> Textures = [];
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
    public static void Release()
    {
        foreach (var texture in Textures.Values) { var source = texture.Atlas; texture.Dispose(); source?.Dispose(); }
        Textures.Clear(); _loaded = false;
    }
    public static void Draw(Control canvas, string definitionId, Rect2 bounds, bool rotated, float opacity = 1)
    {
        Load();
        if (!Textures.TryGetValue(definitionId, out var texture)) return;
        var baseRotation = definitionId == "gum-pack";
        var turn = rotated ^ baseRotation;
        var available = turn ? new Vector2(bounds.Size.Y, bounds.Size.X) : bounds.Size;
        var scale = Math.Min(available.X / texture.GetWidth(), available.Y / texture.GetHeight());
        var size = texture.GetSize() * scale;
        canvas.DrawSetTransform(bounds.GetCenter(), turn ? Mathf.Pi / 2 : 0);
        canvas.DrawTextureRect(texture, new(-size / 2, size), false, new(1, 1, 1, opacity));
        canvas.DrawSetTransform(Vector2.Zero);
    }
}
