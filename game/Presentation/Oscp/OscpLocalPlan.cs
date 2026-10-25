using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Known construction plan and acquired contacts. Never draws hidden live actors or door status.</summary>
public partial class OscpLocalPlan : Control
{
    public SpatialSimulation Simulation { get; set; } = null!;
    private static Vector2 Project(Point2 point)
    { var pixel = ReferenceProjection.Project(point); return new((float)pixel.X, (float)pixel.Y); }
    public override void _Draw()
    {
        var map = Simulation.Map.Definition;
        var corners = new[] { new Point2(0, 0), new(map.Width, 0), new(map.Width, map.Height), new(0, map.Height) }.Select(Project).ToArray();
        var min = new Vector2(corners.Min(p => p.X), corners.Min(p => p.Y));
        var max = new Vector2(corners.Max(p => p.X), corners.Max(p => p.Y));
        var scale = Math.Min((Size.X - 28) / (max.X - min.X), (Size.Y - 30) / (max.Y - min.Y));
        Vector2 At(Point2 point) => (Project(point) - (min + max) / 2) * scale + Size / 2;
        DrawColoredPolygon(corners.Select(p => (p - (min + max) / 2) * scale + Size / 2).ToArray(), new Color("142622"));
        foreach (var wall in map.Walls) DrawLine(At(wall.Start), At(wall.End), new Color(wall.Door ? "c59b61" : "718b7d"), wall.Door ? 3 : 2);
        foreach (var contact in Simulation.Awareness.Evidence)
        {
            var p = At(contact.Position);
            DrawArc(p, 6, 0, Mathf.Tau, 16, new Color("d5ae75"), 1);
        }
        var player = At(Simulation.Player.Position);
        DrawCircle(player, 4, new Color("dce8d5"));
        DrawString(ThemeDB.FallbackFont, player + new Vector2(8, -5), "YOU", HorizontalAlignment.Left, -1, 10, new Color("dce8d5"));
    }
}
