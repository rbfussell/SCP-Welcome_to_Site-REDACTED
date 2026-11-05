using Godot;

namespace SitePresentation;

internal sealed record PaperdollPocket(string Region, string Side, float[][] Polygon);

internal sealed partial class InventoryPaperdollArt
{
    private static void ValidatePockets(PaperdollManifest manifest)
    {
        foreach (var part in manifest.Frames.SelectMany(f => f.Parts))
        {
            var pockets = part.Pockets ?? [];
            if (pockets.Length > 8 || pockets.Select(p => (p.Region, p.Side)).Distinct().Count() != pockets.Length
                || pockets.Any(p => string.IsNullOrWhiteSpace(p.Region) || p.Region.Length > 64 || p.Side is not ("front" or "back")
                    || !PaperdollWornMask.Valid([p.Polygon])))
                throw new InvalidOperationException("Invalid source-local paperdoll pocket segment.");
        }
    }
    /// <summary>Texture-backed polygon tint follows worn alpha and ordinary layer order; source art and access masks stay unchanged.</summary>
    private void DrawPocket(Control canvas, Rect2 bounds, PaperdollLayer layer, PaperdollPart part, string side, PaperdollPocket pocket, bool allowed)
    {
        var dest = Destination(part, side); var raster = Raster(layer, part, side);
        var local = pocket.Polygon.Select(p => new Vector2(p[0], p[1])).ToArray();
        var points = local.Select(p => bounds.Position + (dest.Position + p * dest.Size) * bounds.Size).ToArray();
        if (layer.Mounted is { } mounted)
        {
            canvas.DrawSetTransform(bounds.Position + mounted.Origin * bounds.Size, mounted.Rotation * Mathf.Pi / 3);
            points = local.Select(p => (p - mounted.Anchor) * dest.Size * bounds.Size).ToArray();
        }
        var uvs = local.Select(p => (raster.Source.Position + p * raster.Source.Size) / raster.Texture.GetSize()).ToArray();
        canvas.DrawPolygon(points, new[] { allowed ? new Color(.6f, 1.5f, .75f) : new Color(1.6f, .5f, .45f) }, uvs, raster.Texture);
        if (layer.Mounted is not null) canvas.DrawSetTransform(Vector2.Zero);
    }
}
