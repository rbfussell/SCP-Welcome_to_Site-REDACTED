using Godot;
using System.Text.Json;

namespace SitePresentation;

/// <summary>Authored, normalized hit regions over object art. Geometry never defines storage capacity.</summary>
public partial class InventoryGearSurface : Control
{
    public InventoryPanel Panel { get; set; } = null!;
    public InventoryBrowser Browser { get; set; } = null!;
    public string ItemId { get; set; } = "";
    private sealed record Hotspot(string? Region, string Label, float[] Rect);
    private static Dictionary<string, Hotspot[]>? _bindings;
    private static Dictionary<string, Hotspot[]> Bindings => _bindings ??= JsonSerializer.Deserialize<Dictionary<string, Hotspot[]>>(
        Godot.FileAccess.GetFileAsString("res://Assets/inventory-objects/hotspots.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    internal static bool Registered(string definition) => Bindings.ContainsKey(definition);
    private Hotspot[] Targets => Bindings.GetValueOrDefault(Panel.Simulation.Inventory.Definition(ItemId).Id) ?? [];
    public override void _Ready()
    { CustomMinimumSize = new(330, 350); MouseFilter = MouseFilterEnum.Stop; TextureFilter = TextureFilterEnum.Nearest; }
    private Rect2 Bounds(Hotspot spot) => new(spot.Rect[0] * Size.X, spot.Rect[1] * Size.Y, spot.Rect[2] * Size.X, spot.Rect[3] * Size.Y);
    public override void _Draw()
    {
        InventoryArt.Draw(this, Panel.Simulation.Inventory.Definition(ItemId).Id, new(Vector2.Zero, Size), false);
        foreach (var target in Targets)
        {
            var rect = Bounds(target);
            var item = Panel.Simulation.Inventory.DiagnosticItems.Single(i => i.Id == ItemId);
            var open = target.Region is null ? item.GarmentOpen : Panel.Simulation.Inventory.IsOpen(ItemId, target.Region);
            DrawRect(rect, new Color(open ? "69b68a" : "c6ab73", 0.12f));
            DrawRect(rect, new(open ? "69b68a" : "c6ab73"), false, 1);
            DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(3, 14), target.Label + (open ? " · OPEN" : " · SECURED"), fontSize: 11);
        }
    }
    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true } mouse) return;
        var spot = Targets.FirstOrDefault(t => Bounds(t).HasPoint(mouse.Position));
        if (mouse.ButtonIndex == MouseButton.Right) Panel.Context(ItemId, spot?.Region, GetGlobalMousePosition(), spot is not null && spot.Region is null);
        else if (mouse.ButtonIndex == MouseButton.Left && mouse.DoubleClick && spot is not null)
        {
            if (spot.Region is not null) Panel.OpenCompartment(ItemId, spot.Region);
            else Panel.OpenGarment(ItemId);
        }
        else if (mouse.ButtonIndex == MouseButton.Left) Panel.Select(ItemId);
        AcceptEvent();
    }
    internal Vector2 HotspotPosition(string? region) => GlobalPosition + Bounds(Targets.Single(t => t.Region == region)).GetCenter();
}
