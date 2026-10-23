namespace SiteSimulation;

/// <summary>Continuous position in square construction coordinates, independent of rendering.</summary>
public readonly record struct Point2(double X, double Y)
{
    public static Point2 operator +(Point2 a, Point2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Point2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Point2 operator *(Point2 a, double t) => new(a.X * t, a.Y * t);
    [System.Text.Json.Serialization.JsonIgnore]
    public double Length => Math.Sqrt(X * X + Y * Y);
    public static double Distance(Point2 a, Point2 b) => (a - b).Length;
}

/// <summary>Axial actor grid registered at four hexes per square floor cell.</summary>
public readonly record struct Hex(int Q, int R)
{
    public static readonly Hex[] Directions = [new(1, -1), new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1)];
    [System.Text.Json.Serialization.JsonIgnore]
    public Point2 World => new(Q / 2.0, R / 2.0 + Q / 4.0);
    public Hex Neighbor(int facing) => new(Q + Directions[facing].Q, R + Directions[facing].R);
    public int Distance(Hex other) => (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(Q + R - other.Q - other.R)) / 2;
    public static Hex FromWorld(Point2 p)
    {
        var q = p.X * 2;
        var r = p.Y * 2 - p.X;
        var s = -q - r;
        var iq = (int)Math.Round(q);
        var ir = (int)Math.Round(r);
        var iz = (int)Math.Round(s);
        var dq = Math.Abs(iq - q);
        var dr = Math.Abs(ir - r);
        var dz = Math.Abs(iz - s);
        if (dq > dr && dq > dz) iq = -ir - iz;
        else if (dr > dz) ir = -iq - iz;
        return new(iq, ir);
    }
}

public static class Geometry
{
    public static double DistanceToSegment(Point2 p, Point2 a, Point2 b)
    {
        var ab = b - a;
        var squared = ab.X * ab.X + ab.Y * ab.Y;
        var t = squared == 0 ? 0 : Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / squared, 0, 1);
        return Point2.Distance(p, a + ab * t);
    }

    /// <summary>Closed segment intersection, including touching an endpoint or a collinear overlap.</summary>
    public static bool Intersects(Point2 a, Point2 b, Point2 c, Point2 d)
    {
        static double Cross(Point2 u, Point2 v) => u.X * v.Y - u.Y * v.X;
        var x = Cross(b - a, c - a);
        var y = Cross(b - a, d - a);
        var z = Cross(d - c, a - c);
        var w = Cross(d - c, b - c);
        if (((x > 0 && y < 0) || (x < 0 && y > 0)) && ((z > 0 && w < 0) || (z < 0 && w > 0))) return true;
        return Math.Abs(x) < 1e-9 && DistanceToSegment(c, a, b) < 1e-9
            || Math.Abs(y) < 1e-9 && DistanceToSegment(d, a, b) < 1e-9
            || Math.Abs(z) < 1e-9 && DistanceToSegment(a, c, d) < 1e-9
            || Math.Abs(w) < 1e-9 && DistanceToSegment(b, c, d) < 1e-9;
    }
}
