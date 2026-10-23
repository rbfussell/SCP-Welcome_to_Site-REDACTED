using SiteSimulation;

namespace SitePresentation;

/// <summary>Reference-scale screen mapping, separate from authoritative ground geometry.</summary>
public static class ReferenceProjection
{
    public static Point2 Project(Point2 ground) => new(48 * ground.X + 32 * ground.Y, -12 * ground.X + 24 * ground.Y);
    public static Point2 Unproject(Point2 screen) => new((24 * screen.X - 32 * screen.Y) / 1536, (12 * screen.X + 48 * screen.Y) / 1536);
    public static readonly Point2[] HexCorners = [new(0, -8), new(16, -4), new(16, 4), new(0, 8), new(-16, 4), new(-16, -4)];
}
