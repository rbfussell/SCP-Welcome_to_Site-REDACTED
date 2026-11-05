using Godot;
using SiteSimulation;
using System.Text.Json;

namespace SitePresentation;

internal sealed record PaperdollBinding(string Definition, int Column, int? OpenColumn = null, float[][]? BaseCutouts = null, Dictionary<string, int>? MountColumns = null, PaperdollAttachmentPlane? AttachmentPlane = null);
internal sealed record PaperdollPart(int[] Front, int[] Back, float[] Destination, float[]? BackDestination = null, string? Side = null, float[][][]? FrontWornCutouts = null, float[][][]? BackWornCutouts = null);
internal sealed record PaperdollFrame(int Column, string File, PaperdollPart[] Parts);
internal sealed record PaperdollManifest(int Version, string Actor, string BodyFamily, int[] CanvasSize, Dictionary<string, int[]> Images,
    PaperdollFrame[] Frames, PaperdollBinding[] Bindings, Dictionary<string, float[][]> HeadCutouts, PaperdollMosaicDefinition Mosaic, PaperdollAttachmentBinding[]? Attachments = null, PaperdollBodyPlane? BodyPlane = null);
internal sealed record PaperdollLayer(string? ItemId, int Column, PaperdollMountedArt? Mounted = null);

/// <summary>Registered full-body art fixture. One source/destination transform serves drawing and alpha picking; pixels never grant physical access.</summary>
internal sealed partial class InventoryPaperdollArt : IDisposable
{
    private readonly PaperdollManifest _manifest;
    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly Dictionary<string, Image> _images = [];
    private readonly Dictionary<(PaperdollPart Part, string Side), PaperdollRaster> _worn = [];
    private sealed record PaperdollRaster(Texture2D Texture, Image Pixels, Rect2 Source);
    internal InventoryPaperdollArt()
    {
        _manifest = JsonSerializer.Deserialize<PaperdollManifest>(Godot.FileAccess.GetFileAsString("res://Assets/paperdoll-proof/manifest.json"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Missing paperdoll art manifest.");
        Validate(_manifest);
        ValidateAttachments(_manifest);
        ValidateBodyPlane(_manifest.BodyPlane);
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
            || m.Images.Any(i => !AssetFile(i.Key)
                || i.Value is not { Length: 2 } || i.Value.Any(n => n <= 0))
            || m.Frames is not { Length: > 0 and <= 128 } || m.Frames.Any(f => f.Column is < 0 or > 127)
            || m.Frames.Select(f => f.Column).Distinct().Count() != m.Frames.Length || !m.Frames.Any(f => f.Column == 0)
            || m.Frames.Any(f => !m.Images.ContainsKey(f.File) || f.Parts is not { Length: > 0 and <= 8 }
                || f.Parts.Any(p => p.Side is not (null or "front" or "back") || !Rect(p.Destination) || p.BackDestination is { } back && !Rect(back)
                    || !PaperdollWornMask.Valid(p.FrontWornCutouts) || !PaperdollWornMask.Valid(p.BackWornCutouts)
                    || new[] { p.Front, p.Back }.Any(r => r is not { Length: 4 } || r[0] < 0 || r[1] < 0 || r[2] <= 0 || r[3] <= 0
                        || (long)r[0] + r[2] > m.Images[f.File][0] || (long)r[1] + r[3] > m.Images[f.File][1])))
            || m.Frames.Single(f => f.Column == 0).Parts.Length != 1 || m.Frames.Single(f => f.Column == 0).Parts[0].Side is not null
            || m.Frames.Single(f => f.Column == 0).Parts.Any(p => p.FrontWornCutouts is not null || p.BackWornCutouts is not null)
            || m.HeadCutouts is null || !new[] { "front", "back" }.All(side => m.HeadCutouts.TryGetValue(side, out var polygon)
                && polygon.Length >= 3 && polygon.All(p => p is { Length: 2 } && p.All(n => float.IsFinite(n) && n is >= 0 and <= 1)))
            || m.Mosaic is null || !Rect(m.Mosaic.Front) || !Rect(m.Mosaic.Back) || m.Mosaic.BlockSize is < 8 or > 40
            || m.Bindings is not { Length: > 0 and <= 128 } || m.Bindings.Select(b => b.Definition).Distinct().Count() != m.Bindings.Length
            || m.Bindings.Any(b => string.IsNullOrWhiteSpace(b.Definition) || b.Column <= 0 || !m.Frames.Any(f => f.Column == b.Column)
                || (b.OpenColumn is { } open && (open <= 0 || !m.Frames.Any(f => f.Column == open)))
                || b.BaseCutouts is { } cuts && cuts.Any(r => !Rect(r))
                || b.MountColumns is { } mounts && (mounts.Count == 0 || mounts.Any(pair => string.IsNullOrWhiteSpace(pair.Key)
                    || pair.Value <= 0 || !m.Frames.Any(f => f.Column == pair.Value)))))
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
            var column = item.GarmentOpen ? binding.OpenColumn ?? binding.Column : binding.Column;
            if (binding.MountColumns is { } mounts)
            {
                var slot = sim.Inventory.Region(item.Placement.ParentId, item.Placement.RegionId).MountSlot!;
                if (!mounts.TryGetValue(slot, out column)) continue; // Unqualified mounts stay explicitly omitted.
            }
            result.Add(new(item.Id, column));
            result.AddRange(AttachedLayers(sim, item, binding));
        }
        return result.ToArray();
    }
    internal string[] Omitted(SpatialSimulation sim)
    {
        var drawn = Layers(sim).Select(l => l.ItemId).OfType<string>().ToHashSet();
        return OwnExterior(sim).Where(i => !drawn.Contains(i.Id))
            .Select(i => sim.Inventory.Definition(i.Id).Name).ToArray();
    }
    private PaperdollFrame Frame(PaperdollLayer layer) => _manifest.Frames.Single(f => f.Column == layer.Column);
    /// <summary>Side-specific overlays share one filter for rendering and alpha picking.</summary>
    private IEnumerable<PaperdollPart> Parts(PaperdollLayer layer, string side)
        => Frame(layer).Parts.Where(p => (p.Side is null || p.Side == side) && (layer.Mounted is null || layer.Mounted.Surface == side));
    private static Rect2 Source(PaperdollPart part, string side)
    { var r = side == "back" ? part.Back : part.Front; return new(r[0], r[1], r[2], r[3]); }
    private static Rect2 Destination(PaperdollPart part, string side)
    { var r = side == "back" ? part.BackDestination ?? part.Destination : part.Destination; return new(r[0], r[1], r[2], r[3]); }
    /// <summary>Only worn paperdoll parts use these cached derived pixels. Loose art/drag previews never consult this cache.</summary>
    private PaperdollRaster Raster(PaperdollLayer layer, PaperdollPart part, string side)
    {
        var file = Frame(layer).File; var source = Source(part, side);
        var cuts = side == "back" ? part.BackWornCutouts : part.FrontWornCutouts;
        if (cuts is not { Length: > 0 }) return new(_textures[file], _images[file], source);
        if (_worn.TryGetValue((part, side), out var cached)) return cached;
        var pixels = _images[file].GetRegion(new((int)source.Position.X, (int)source.Position.Y, (int)source.Size.X, (int)source.Size.Y));
        PaperdollWornMask.Apply(pixels, cuts);
        var raster = new PaperdollRaster(ImageTexture.CreateFromImage(pixels), pixels, new(Vector2.Zero, source.Size));
        _worn.Add((part, side), raster); return raster;
    }
    internal Rect2 Bounds(Vector2 size)
    {
        var source = new Vector2(_manifest.CanvasSize[0], _manifest.CanvasSize[1]);
        var scaled = source * Math.Min(size.X / source.X, size.Y / source.Y); return new((size - scaled) / 2, scaled);
    }
    internal bool Opaque(PaperdollLayer layer, string side, Vector2 uv, bool worn = true)
    {
        foreach (var part in Parts(layer, side))
        {
            var dest = Destination(part, side); var local = layer.Mounted is { } mounted
                ? MountedLocal(mounted, dest, uv) : (uv - dest.Position) / dest.Size;
            if (local.X is < 0 or >= 1 || local.Y is < 0 or >= 1) continue;
            var raster = worn ? Raster(layer, part, side) : new PaperdollRaster(_textures[Frame(layer).File], _images[Frame(layer).File], Source(part, side));
            var pixel = raster.Source.Position + local * raster.Source.Size;
            if (raster.Pixels.GetPixel((int)pixel.X, (int)pixel.Y).A > .1f) return true;
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
    /// <summary>Visual exposure only; neither this overlay nor garment alpha changes physical access or item identity.</summary>
    internal bool MosaicVisible(SpatialSimulation sim, string side, Vector2 uv)
        => PaperdollMosaic.Region(_manifest.Mosaic, side).HasPoint(uv) && Layers(sim) is { Length: > 0 } layers
            && !layers.Any(l => l.ItemId is not null && Opaque(l, side, uv));
    private void DrawMosaic(Control canvas, Rect2 bounds, string side)
    {
        var body = Frame(new(null, 0)); var part = body.Parts.Single();
        var source = Source(part, side); var dest = Destination(part, side);
        foreach (var tile in PaperdollMosaic.Tiles(_manifest.Mosaic, side, new(_manifest.CanvasSize[0], _manifest.CanvasSize[1])))
        {
            var sample = source.Position + (tile.GetCenter() - dest.Position) / dest.Size * source.Size;
            var color = _images[body.File].GetPixel((int)sample.X, (int)sample.Y);
            // Opaque coarse blocks are synthesized by Godot; no mosaic is stored in the source image.
            color = new(MathF.Round(color.R * 12) / 12, MathF.Round(color.G * 12) / 12, MathF.Round(color.B * 12) / 12, 1);
            canvas.DrawRect(new(bounds.Position + tile.Position * bounds.Size, tile.Size * bounds.Size), color);
        }
    }
    internal void Draw(Control canvas, SpatialSimulation sim, string side, string? hover, string? selected)
    {
        var layers = Layers(sim); if (layers.Length == 0) return;
        var bounds = Bounds(canvas.Size);
        foreach (var layer in layers)
        {
            var tint = layer.ItemId is not null && layer.ItemId == hover ? new Color(1.3f, 1.5f, 1.25f)
                : layer.ItemId is not null && layer.ItemId == selected ? new Color(1.15f, 1.25f, 1.1f) : Colors.White;
            foreach (var part in Parts(layer, side))
            {
                var dest = Destination(part, side); var raster = Raster(layer, part, side); var sourcePart = raster.Source;
                if (layer.Mounted is { } mounted)
                { DrawMounted(canvas, bounds, dest, raster.Texture, raster.Source, mounted, tint); continue; }
                foreach (var clip in layer.ItemId is null ? BaseRegions(layers) : new[] { dest })
                {
                    var visible = dest.Intersection(clip); if (!visible.HasArea()) continue;
                    var sourceClip = new Rect2(sourcePart.Position + (visible.Position - dest.Position) / dest.Size * sourcePart.Size,
                        visible.Size / dest.Size * sourcePart.Size);
                    canvas.DrawTextureRectRegion(raster.Texture, new(bounds.Position + visible.Position * bounds.Size, visible.Size * bounds.Size), sourceClip, tint);
                }
            }
            if (layer.ItemId is null) DrawMosaic(canvas, bounds, side);
        }
        var body = new PaperdollLayer(null, 0); var bodyPart = Frame(body).Parts.Single();
        var source = Source(bodyPart, side); var destBody = Destination(bodyPart, side); var head = Head(side);
        var texture = _textures[Frame(body).File];
        canvas.DrawPolygon(head.Select(uv => bounds.Position + uv * bounds.Size).ToArray(), new[] { Colors.White },
            head.Select(uv => (source.Position + (uv - destBody.Position) / destBody.Size * source.Size) / texture.GetSize()).ToArray(), texture);
    }
    public void Dispose()
    {
        foreach (var raster in _worn.Values) { raster.Texture.Dispose(); raster.Pixels.Dispose(); } _worn.Clear();
        foreach (var image in _images.Values) image.Dispose(); foreach (var texture in _textures.Values) texture.Dispose();
        _images.Clear(); _textures.Clear();
    }
}
