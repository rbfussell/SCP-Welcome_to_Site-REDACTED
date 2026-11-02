using Godot;
using SiteSimulation;
using System.Text.Json;

namespace SitePresentation;

internal sealed record PaperdollBinding(string Definition, int Column, int? OpenColumn = null, float[][]? BaseCutouts = null);
internal sealed record PaperdollPart(int[] Front, int[] Back, float[] Destination, float[]? BackDestination = null);
internal sealed record PaperdollFrame(int Column, string File, PaperdollPart[] Parts);
internal sealed record PaperdollManifest(int Version, string Actor, string BodyFamily, int[] CanvasSize, Dictionary<string, int[]> Images,
    PaperdollFrame[] Frames, PaperdollBinding[] Bindings, Dictionary<string, float[][]> HeadCutouts);
internal sealed record PaperdollLayer(string? ItemId, int Column);

/// <summary>Registered full-body art fixture. One source/destination transform serves drawing and alpha picking; pixels never grant physical access.</summary>
internal sealed class InventoryPaperdollArt : IDisposable
{
    private readonly PaperdollManifest _manifest;
    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly Dictionary<string, Image> _images = [];
    internal InventoryPaperdollArt()
    {
        _manifest = JsonSerializer.Deserialize<PaperdollManifest>(Godot.FileAccess.GetFileAsString("res://Assets/paperdoll-proof/manifest.json"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Missing paperdoll art manifest.");
        Validate(_manifest);
        foreach (var (file, size) in _manifest.Images)
        {
            var texture = GD.Load<Texture2D>("res://Assets/paperdoll-proof/" + file);
            if (texture.GetWidth() != size[0] || texture.GetHeight() != size[1]) throw new InvalidOperationException("Paperdoll image size differs: " + file);
            _textures.Add(file, texture); _images.Add(file, texture.GetImage());
        }
    }
    private static void Validate(PaperdollManifest m)
    {
        static bool Rect(float[]? r) => r is { Length: 4 } && r.All(float.IsFinite) && r[0] >= 0 && r[1] >= 0
            && r[2] > 0 && r[3] > 0 && r[0] + r[2] <= 1.001f && r[1] + r[3] <= 1.001f;
        if (m.Version != 2 || m.Actor != "avatar" || m.BodyFamily != "human-nine-shoulder"
            || m.CanvasSize is not { Length: 2 } || m.CanvasSize.Any(n => n <= 0) || m.Images is not { Count: > 0 }
            || m.Images.Any(i => System.IO.Path.GetFileName(i.Key) != i.Key || !i.Key.EndsWith(".png", StringComparison.Ordinal)
                || i.Value is not { Length: 2 } || i.Value.Any(n => n <= 0))
            || m.Frames is not { Length: > 0 and <= 128 } || m.Frames.Any(f => f.Column is < 0 or > 127)
            || m.Frames.Select(f => f.Column).Distinct().Count() != m.Frames.Length || !m.Frames.Any(f => f.Column == 0)
            || m.Frames.Any(f => !m.Images.ContainsKey(f.File) || f.Parts is not { Length: > 0 and <= 8 }
                || f.Parts.Any(p => !Rect(p.Destination) || p.BackDestination is { } back && !Rect(back)
                    || new[] { p.Front, p.Back }.Any(r => r is not { Length: 4 } || r[0] < 0 || r[1] < 0 || r[2] <= 0 || r[3] <= 0
                        || (long)r[0] + r[2] > m.Images[f.File][0] || (long)r[1] + r[3] > m.Images[f.File][1])))
            || m.Frames.Single(f => f.Column == 0).Parts.Length != 1
            || m.HeadCutouts is null || !new[] { "front", "back" }.All(side => m.HeadCutouts.TryGetValue(side, out var polygon)
                && polygon.Length >= 3 && polygon.All(p => p is { Length: 2 } && p.All(n => float.IsFinite(n) && n is >= 0 and <= 1)))
            || m.Bindings is not { Length: > 0 and <= 128 } || m.Bindings.Select(b => b.Definition).Distinct().Count() != m.Bindings.Length
            || m.Bindings.Any(b => string.IsNullOrWhiteSpace(b.Definition) || b.Column <= 0 || !m.Frames.Any(f => f.Column == b.Column)
                || (b.OpenColumn is { } open && (open <= 0 || !m.Frames.Any(f => f.Column == open)))
                || b.BaseCutouts is { } cuts && cuts.Any(r => !Rect(r))))
            throw new InvalidOperationException("Invalid bounded full-body paperdoll manifest.");
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
            result.Add(new(item.Id, item.GarmentOpen ? binding.OpenColumn ?? binding.Column : binding.Column));
        }
        return result.ToArray();
    }
    internal string[] Omitted(SpatialSimulation sim)
    {
        var drawn = Layers(sim).Select(l => l.ItemId).OfType<string>().ToHashSet();
        return sim.BodyFitItems.Where(i => i.Placement.RegionId != "hand" && !drawn.Contains(i.Id))
            .Select(i => sim.Inventory.Definition(i.Id).Name).ToArray();
    }
    private PaperdollFrame Frame(PaperdollLayer layer) => _manifest.Frames.Single(f => f.Column == layer.Column);
    private static Rect2 Source(PaperdollPart part, string side)
    { var r = side == "back" ? part.Back : part.Front; return new(r[0], r[1], r[2], r[3]); }
    private static Rect2 Destination(PaperdollPart part, string side)
    { var r = side == "back" ? part.BackDestination ?? part.Destination : part.Destination; return new(r[0], r[1], r[2], r[3]); }
    internal Rect2 Bounds(Vector2 size)
    {
        var source = new Vector2(_manifest.CanvasSize[0], _manifest.CanvasSize[1]);
        var scaled = source * Math.Min(size.X / source.X, size.Y / source.Y); return new((size - scaled) / 2, scaled);
    }
    internal bool Opaque(PaperdollLayer layer, string side, Vector2 uv)
    {
        foreach (var part in Frame(layer).Parts)
        {
            var dest = Destination(part, side); var local = (uv - dest.Position) / dest.Size;
            if (local.X is < 0 or >= 1 || local.Y is < 0 or >= 1) continue;
            var source = Source(part, side); var pixel = source.Position + local * source.Size;
            if (_images[Frame(layer).File].GetPixel((int)pixel.X, (int)pixel.Y).A > .1f) return true;
        }
        return false;
    }
    private Vector2[] Head(string side) => _manifest.HeadCutouts[side].Select(p => new Vector2(p[0], p[1])).ToArray();
    internal bool HeadOpaque(string side, Vector2 uv) => Geometry2D.IsPointInPolygon(uv, Head(side)) && Opaque(new(null, 0), side, uv);
    internal string? Pick(SpatialSimulation sim, string side, Vector2 size, Vector2 position)
    {
        var bounds = Bounds(size); var uv = (position - bounds.Position) / bounds.Size;
        if (HeadOpaque(side, uv)) return null;
        return Layers(sim).Reverse().FirstOrDefault(l => Opaque(l, side, uv))?.ItemId;
    }
    /// <summary>Authored stand-in body cutouts prevent bare arms clipping through opaque long sleeves. No physical surface is changed.</summary>
    private Rect2[] BaseRegions(PaperdollLayer[] layers)
    {
        var columns = layers.Where(l => l.ItemId is not null).Select(l => l.Column).ToHashSet();
        var cuts = _manifest.Bindings.Where(b => columns.Contains(b.Column) || b.OpenColumn is { } open && columns.Contains(open))
            .SelectMany(b => b.BaseCutouts ?? []).Select(r => new Rect2(r[0], r[1], r[2], r[3])).Distinct().ToArray();
        if (cuts.Length == 0) return [new(0, 0, 1, 1)];
        var xs = cuts.SelectMany(r => new[] { r.Position.X, r.End.X }).Concat(new float[] { 0, 1 }).Distinct().Order().ToArray();
        var ys = cuts.SelectMany(r => new[] { r.Position.Y, r.End.Y }).Concat(new float[] { 0, 1 }).Distinct().Order().ToArray();
        var result = new List<Rect2>();
        for (var x = 0; x < xs.Length - 1; x++)
        for (var y = 0; y < ys.Length - 1; y++)
        {
            var r = new Rect2(xs[x], ys[y], xs[x + 1] - xs[x], ys[y + 1] - ys[y]);
            if (!cuts.Any(c => c.HasPoint(r.GetCenter()))) result.Add(r);
        }
        return result.ToArray();
    }
    internal void Draw(Control canvas, SpatialSimulation sim, string side, string? hover, string? selected)
    {
        var layers = Layers(sim); if (layers.Length == 0) return;
        var bounds = Bounds(canvas.Size);
        foreach (var layer in layers)
        {
            var tint = layer.ItemId is not null && layer.ItemId == hover ? new Color(1.3f, 1.5f, 1.25f)
                : layer.ItemId is not null && layer.ItemId == selected ? new Color(1.15f, 1.25f, 1.1f) : Colors.White;
            foreach (var part in Frame(layer).Parts)
            {
                var dest = Destination(part, side); var sourcePart = Source(part, side);
                foreach (var clip in layer.ItemId is null ? BaseRegions(layers) : new[] { dest })
                {
                    var visible = dest.Intersection(clip); if (!visible.HasArea()) continue;
                    var sourceClip = new Rect2(sourcePart.Position + (visible.Position - dest.Position) / dest.Size * sourcePart.Size,
                        visible.Size / dest.Size * sourcePart.Size);
                    canvas.DrawTextureRectRegion(_textures[Frame(layer).File], new(bounds.Position + visible.Position * bounds.Size, visible.Size * bounds.Size), sourceClip, tint);
                }
            }
        }
        var body = new PaperdollLayer(null, 0); var bodyPart = Frame(body).Parts.Single();
        var source = Source(bodyPart, side); var destBody = Destination(bodyPart, side); var head = Head(side);
        var texture = _textures[Frame(body).File];
        canvas.DrawPolygon(head.Select(uv => bounds.Position + uv * bounds.Size).ToArray(), new[] { Colors.White },
            head.Select(uv => (source.Position + (uv - destBody.Position) / destBody.Size * source.Size) / texture.GetSize()).ToArray(), texture);
    }
    public void Dispose()
    {
        foreach (var image in _images.Values) image.Dispose(); foreach (var texture in _textures.Values) texture.Dispose();
        _images.Clear(); _textures.Clear();
    }
}
