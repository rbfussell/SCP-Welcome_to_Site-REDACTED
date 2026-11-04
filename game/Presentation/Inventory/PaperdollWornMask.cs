using Godot;

namespace SitePresentation;

/// <summary>Authored visual holes in a worn part, normalized to its source rectangle; never physical occupancy or access masks.</summary>
internal static class PaperdollWornMask
{
    internal static bool Valid(float[][][]? cutouts) => cutouts is null || cutouts.Length <= 8 && cutouts.All(p =>
        p is { Length: >= 3 and <= 32 } && p.All(v => v is { Length: 2 } && v.All(n => float.IsFinite(n) && n is >= 0 and <= 1))
        && p.Select(v => new Vector2(v[0], v[1])).Distinct().Count() == p.Length
        && Math.Abs(p.Select((v, i) => (double)v[0] * p[(i + 1) % p.Length][1] - (double)v[1] * p[(i + 1) % p.Length][0]).Sum()) > 0
        && Geometry2D.TriangulatePolygon(p.Select(v => new Vector2(v[0], v[1])).ToArray()).Length > 0);

    /// <summary>Clear only authored hole alpha on a private cropped copy; original RGB/alpha outside holes is preserved.</summary>
    internal static void Apply(Image image, float[][][] cutouts)
    {
        if (!Valid(cutouts)) throw new InvalidOperationException("Invalid worn-only alpha cutouts.");
        var polygons = cutouts.Select(p => p.Select(v => new Vector2(v[0], v[1])).ToArray()).ToArray();
        var width = image.GetWidth(); var height = image.GetHeight();
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var uv = new Vector2((x + .5f) / width, (y + .5f) / height);
            if (!polygons.Any(p => Geometry2D.IsPointInPolygon(uv, p))) continue;
            var color = image.GetPixel(x, y); color.A = 0; image.SetPixel(x, y, color);
        }
    }
}
