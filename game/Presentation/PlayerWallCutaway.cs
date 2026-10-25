using SiteSimulation;

namespace SitePresentation;

/// <summary>A cosmetic screen-space opening. It never queries or changes observation or collision.</summary>
public readonly record struct PlayerWallCutaway(Point2 Center, double Radius)
{
    public const double DiameterInCharacterHeights = 2;
    public const double ClearCoreFraction = 0.45;
    public const double MinimumOpacity = 0.06;

    public static PlayerWallCutaway AroundSprite(Point2 feet, double spriteHeight)
    {
        var height = Math.Max(24, spriteHeight);
        return new(new(feet.X, feet.Y - height * 0.5), height * DiameterInCharacterHeights * 0.5);
    }

    public double Opacity(Point2 pixel)
    {
        var t = Math.Clamp((Point2.Distance(pixel, Center) / Radius - ClearCoreFraction) / (1 - ClearCoreFraction), 0, 1);
        return MinimumOpacity + (1 - MinimumOpacity) * t * t * (3 - 2 * t);
    }

    public bool Intersects(double left, double top, double right, double bottom)
        => Point2.Distance(Center, new(Math.Clamp(Center.X, left, right), Math.Clamp(Center.Y, top, bottom))) < Radius;
}
