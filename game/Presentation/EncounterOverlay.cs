using Godot;

namespace SitePresentation;

/// <summary>Subtle field vignette and caption. No shake, flashes, or world-state effects.</summary>
public partial class EncounterOverlay : Control
{
    public double Pulse { get; set; }
    public bool ReducedEffects { get; set; }
    public string Caption { get; set; } = "";
    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;
    public override void _Draw()
    {
        // Broad stepped edge shading stays outside the central readable area.
        for (var i = 0; i < 14; i++)
        {
            var margin = i * 5f;
            DrawRect(new Rect2(margin, margin, Size.X - margin * 2, Size.Y - margin * 2),
                new Color(0.015f, 0.02f, 0.025f, 0.035f), false, 10);
        }
        if (!ReducedEffects && Pulse > 0)
            DrawRect(new Rect2(3, 3, Size.X - 6, Size.Y - 6), new Color(0.65f, 0.51f, 0.38f, (float)Pulse * 0.32f), false, 3);
        if (Caption.Length == 0) return;
        var font = ThemeDB.FallbackFont;
        var width = font.GetStringSize(Caption, HorizontalAlignment.Left, -1, 19).X;
        var p = new Vector2((Size.X - width) / 2, Size.Y - 45);
        DrawRect(new Rect2(p.X - 20, p.Y - 25, width + 40, 40), new Color(0.025f, 0.035f, 0.035f, 0.92f));
        DrawString(font, p, Caption, HorizontalAlignment.Left, -1, 19, new Color("ddc9af"));
    }
}
