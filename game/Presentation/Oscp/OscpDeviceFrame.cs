using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Original faux hardware surround. Screen content remains ordinary readable controls.</summary>
public partial class OscpDeviceFrame : Control
{
    public OscpDevice Device { get; set; }
    public float Brightness { get; set; } = 1;
    public Rect2 ScreenArea => Device == OscpDevice.Phone ? new(24, 65, Size.X - 48, Size.Y - 125) : new(68, 48, Size.X - 136, Size.Y - 142);
    private void Box(Rect2 rect, string color, int radius, string? border = null, int width = 0)
    {
        using var style = new StyleBoxFlat { BgColor = new(color), CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius, BorderColor = new(border ?? color),
            BorderWidthLeft = width, BorderWidthTop = width, BorderWidthRight = width, BorderWidthBottom = width };
        DrawStyleBox(style, rect);
    }
    public override void _Draw()
    {
        var phone = Device == OscpDevice.Phone;
        Box(new(new Vector2(7, 10), Size), "050606", phone ? 34 : 25);
        Box(new(Vector2.Zero, Size), phone ? "272e31" : "3b3e3b", phone ? 30 : 22, "666b63", 2);
        Box(new(new Vector2(8, 8), Size - new Vector2(16, 16)), phone ? "171f23" : "2c302e", phone ? 24 : 17, "1b211e", 3);
        var screen = ScreenArea;
        Box(screen.Grow(11), "111615", phone ? 16 : 20, "62675d", 2);
        Box(screen.Grow(4), "040a09", 8);
        DrawRect(screen, new Color("0b1312") * new Color(Brightness, Brightness, Brightness));
        var font = ThemeDB.FallbackFont;
        DrawString(font, new(phone ? 32 : 33, phone ? 34 : Size.Y - 51), phone ? "FOUNDATION  /  SECURE FIELD CLIENT" : "FOUNDATION     INFORMATION SYSTEMS",
            HorizontalAlignment.Left, -1, phone ? 11 : 13, new Color("bec1ad"));
        if (phone)
        {
            Box(new(Size.X / 2 - 25, 43, 50, 4), "070a09", 2);
            Box(new(Size.X / 2 - 30, Size.Y - 25, 60, 4), "718278", 2);
        }
        else
        {
            for (var x = 34; x < 175; x += 7) DrawLine(new(x, Size.Y - 36), new(x + 2, Size.Y - 21), new Color("171d19"), 3);
            DrawCircle(new(Size.X - 44, Size.Y - 41), 13, new Color("151b17"));
            DrawCircle(new(Size.X - 44, Size.Y - 41), 9, new Color("6c7168"));
            DrawCircle(new(Size.X - 83, Size.Y - 41), 3, new Color("a8c78a"));
            DrawString(font, new(Size.X - 184, Size.Y - 20), "OSCP  /  SITE-OPS", HorizontalAlignment.Left, -1, 10, new Color("929c8e"));
        }
        DrawLine(screen.Position + new Vector2(7, 2), new(screen.End.X - 7, screen.Position.Y + 2), new Color(0.8f, 0.9f, 0.8f, 0.09f), 2);
    }
}
