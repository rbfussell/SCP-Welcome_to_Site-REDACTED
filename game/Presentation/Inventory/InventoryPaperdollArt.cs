using Godot;
using SiteSimulation;
using System.Text.Json;

namespace SitePresentation;

internal sealed record PaperdollBinding(string Definition, int Column);
internal sealed record PaperdollFrame(int Column, int[] Front, int[] Back);
internal sealed record PaperdollManifest(int Version, string Actor, string BodyFamily, string File, int[] ImageSize, int[] CanvasSize, PaperdollFrame[] Frames, PaperdollBinding[] Bindings, Dictionary<string, float[][]> HeadCutouts);
internal sealed record PaperdollLayer(string? ItemId, int Column);

/// <summary>Bounded upper-body art fixture. One atlas transform serves drawing and alpha picking; pixels never grant physical access.</summary>
internal sealed class InventoryPaperdollArt : IDisposable
{
    private readonly PaperdollManifest _manifest;
    private readonly Texture2D _texture;
    private readonly Image _image;
    internal InventoryPaperdollArt()
    {
        _manifest = JsonSerializer.Deserialize<PaperdollManifest>(Godot.FileAccess.GetFileAsString("res://Assets/paperdoll-proof/manifest.json"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Missing paperdoll art manifest.");
        var m = _manifest;
        if (m.Version != 1 || m.Actor != "avatar" || m.BodyFamily != "human-nine-shoulder" || m.File != "upper-body.png"
            || m.ImageSize is not { Length: 2 } || m.ImageSize.Any(n => n <= 0)
            || m.CanvasSize is not { Length: 2 } || m.CanvasSize.Any(n => n <= 0)
            || m.Frames is not { Length: 4 } || !m.Frames.Select(f => f.Column).Order().SequenceEqual(new[] { 0, 1, 2, 3 })
            || m.Frames.SelectMany(f => new[] { f.Front, f.Back }).Any(r => r is not { Length: 4 } || r[0] < 0 || r[1] < 0 || r[2] <= 0 || r[3] <= 0
                || (long)r[0] + r[2] > m.ImageSize[0] || (long)r[1] + r[3] > m.ImageSize[1]) || m.HeadCutouts is null || !new[] { "front", "back" }.All(side => m.HeadCutouts.TryGetValue(side, out var polygon)
                && polygon.Length >= 3 && polygon.All(p => p is { Length: 2 } && p.All(n => float.IsFinite(n) && n is >= 0 and <= 1)))
            || m.Bindings is not { Length: 2 }
            || m.Bindings.Select(b => b.Definition).Distinct().Count() != 2 || m.Bindings.Any(b => b.Column is < 1 or > 2))
            throw new InvalidOperationException("Invalid bounded paperdoll art manifest.");
        _texture = GD.Load<Texture2D>("res://Assets/paperdoll-proof/" + m.File); _image = _texture.GetImage();
        if (_texture.GetWidth() != m.ImageSize[0] || _texture.GetHeight() != m.ImageSize[1])
            throw new InvalidOperationException("Paperdoll atlas does not match its registration cells.");
    }
    internal PaperdollLayer[] Layers(SpatialSimulation sim)
    {
        var root = sim.Inventory.Content!.Roots.Single(r => r.ActorId == sim.Player.Id);
        if (root.ActorId != _manifest.Actor || root.BodyFamily != _manifest.BodyFamily) return [];
        var result = new List<PaperdollLayer> { new(null, 0) };
        foreach (var item in sim.BodyFitItems.Where(i => i.Placement.ParentId == root.Id
            && sim.Inventory.Region(i.Placement.ParentId, i.Placement.RegionId).MountSlot is { } slot
            && InventoryWear.Accepts(sim.Inventory.Definition(i.Id), slot)).OrderBy(i => i.Placement.WornOrder))
        {
            var binding = _manifest.Bindings.SingleOrDefault(b => b.Definition == sim.Inventory.Definition(item.Id).Id);
            var shape = InventoryBodyFit.Shape(sim.Inventory, item.Id);
            if (binding is null || shape is null || InventoryBodyFit.Pose(sim.Inventory, item) != shape.DefaultPose) continue;
            result.Add(new(item.Id, binding.Column == 2 && item.GarmentOpen ? 3 : binding.Column));
        }
        return result.ToArray();
    }
    internal string[] Omitted(SpatialSimulation sim)
    {
        var drawn = Layers(sim).Select(l => l.ItemId).OfType<string>().ToHashSet();
        return sim.BodyFitItems.Where(i => i.Placement.RegionId != "hand" && !drawn.Contains(i.Id))
            .Select(i => sim.Inventory.Definition(i.Id).Name).ToArray();
    }
    private Rect2 Source(PaperdollLayer layer, string side)
    {
        var frame = _manifest.Frames.Single(f => f.Column == layer.Column);
        var r = side == "back" ? frame.Back : frame.Front; return new(r[0], r[1], r[2], r[3]);
    }
    internal Rect2 Bounds(Vector2 size)
    {
        var source = new Vector2(_manifest.CanvasSize[0], _manifest.CanvasSize[1]);
        var scaled = source * Math.Min(size.X / source.X, size.Y / source.Y);
        return new((size - scaled) / 2, scaled);
    }
    internal bool Opaque(PaperdollLayer layer, string side, Vector2 uv)
    {
        if (uv.X is < 0 or >= 1 || uv.Y is < 0 or >= 1) return false;
        var source = Source(layer, side); var pixel = source.Position + uv * source.Size;
        return _image.GetPixel((int)pixel.X, (int)pixel.Y).A > .1f;
    }
    private Vector2[] Head(string side) => _manifest.HeadCutouts[side].Select(p => new Vector2(p[0], p[1])).ToArray();
    internal bool HeadOpaque(string side, Vector2 uv) => Geometry2D.IsPointInPolygon(uv, Head(side)) && Opaque(new(null, 0), side, uv);
    internal string? Pick(SpatialSimulation sim, string side, Vector2 size, Vector2 position)
    {
        var bounds = Bounds(size); var uv = (position - bounds.Position) / bounds.Size;
        if (HeadOpaque(side, uv)) return null;
        // Body is an occluder too, but never an inventory target. Transparent garment holes reveal lower layers.
        return Layers(sim).Reverse().FirstOrDefault(l => Opaque(l, side, uv))?.ItemId;
    }
    internal void Draw(Control canvas, SpatialSimulation sim, string side, string? hover, string? selected)
    {
        var layers = Layers(sim); if (layers.Length == 0) return;
        var bounds = Bounds(canvas.Size);
        foreach (var layer in layers)
        {
            var tint = layer.ItemId is not null && layer.ItemId == hover ? new Color(1.3f, 1.5f, 1.25f)
                : layer.ItemId is not null && layer.ItemId == selected ? new Color(1.15f, 1.25f, 1.1f) : Colors.White;
            canvas.DrawTextureRectRegion(_texture, bounds, Source(layer, side), tint);
        }
        // Authored head/neck foreground prevents the far collar from painting over the face.
        var source = Source(new(null, 0), side); var head = Head(side);
        canvas.DrawPolygon(head.Select(uv => bounds.Position + uv * bounds.Size).ToArray(), new[] { Colors.White },
            head.Select(uv => (source.Position + uv * source.Size) / _texture.GetSize()).ToArray(), _texture);
    }
    public void Dispose() { _image.Dispose(); _texture.Dispose(); }
}
