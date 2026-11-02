using Godot;

namespace SitePresentation;

internal sealed record PaperdollMosaicDefinition(float[] Front, float[] Back, int BlockSize);

/// <summary>Authored privacy area with engine-generated coarse blocks. It is presentation, never an inventory layer.</summary>
internal static class PaperdollMosaic
{
    internal static Rect2 Region(PaperdollMosaicDefinition definition, string side)
    {
        var r = side == "back" ? definition.Back : definition.Front;
        return new(r[0], r[1], r[2], r[3]);
    }
    /// <summary>Canvas-relative blocks share the body letterbox transform; edge blocks end exactly at the authored area.</summary>
    internal static IEnumerable<Rect2> Tiles(PaperdollMosaicDefinition definition, string side, Vector2 canvasSize)
    {
        var area = Region(definition, side); var step = new Vector2(definition.BlockSize, definition.BlockSize) / canvasSize;
        for (var y = area.Position.Y; y < area.End.Y; y += step.Y)
        for (var x = area.Position.X; x < area.End.X; x += step.X)
            yield return new(x, y, Math.Min(step.X, area.End.X - x), Math.Min(step.Y, area.End.Y - y));
    }
}
