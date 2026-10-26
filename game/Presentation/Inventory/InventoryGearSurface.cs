using Godot;
using System.Text.Json;

namespace SitePresentation;

/// <summary>Hand-traced source-image segments. Selection and hover share one shape and sprite transform.</summary>
public partial class InventoryGearSurface : Control
{
    public InventoryPanel Panel { get; set; } = null!;
    public InventoryBrowser Browser { get; set; } = null!;
    public string ItemId { get; set; } = "";
    private sealed record Hotspot(string? Region, string Label, float[][] Polygon, float[] HitPoint)
    {
        public Vector2[] Points { get; } = Polygon.Select(p => new Vector2(p[0], p[1])).ToArray();
    }
    private static Dictionary<string, Hotspot[]>? _bindings;
    private static Dictionary<string, Hotspot[]> Bindings => _bindings ??= JsonSerializer.Deserialize<Dictionary<string, Hotspot[]>>(
        Godot.FileAccess.GetFileAsString("res://Assets/inventory-objects/hotspots.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    internal static bool Registered(string definition) => Bindings.ContainsKey(definition);
    private string DefinitionId => Panel.Simulation.Inventory.Definition(ItemId).Id;
    private Hotspot[] Targets => Bindings.GetValueOrDefault(DefinitionId) ?? [];
    private Rect2 Bounds => new(Vector2.Zero, Size);
    private Hotspot? _hovered;
    public override void _Ready()
    {
        CustomMinimumSize = new(330, 350); MouseFilter = MouseFilterEnum.Stop; TextureFilter = TextureFilterEnum.Nearest;
        MouseExited += () => SetHover(null);
        Resized += () => { SetHover(null); QueueRedraw(); };
    }
    private Hotspot? Pick(Vector2 position)
    {
        var source = InventoryArt.Unproject(DefinitionId, Bounds, position);
        if (!InventoryArt.Opaque(DefinitionId, source)) return null;
        // Later entries win where a small visible feature lies over a larger segment.
        return Targets.LastOrDefault(t => Geometry2D.IsPointInPolygon(source, t.Points));
    }
    private void SetHover(Hotspot? target)
    {
        _hovered = target;
        MouseDefaultCursorShape = target is null ? CursorShape.Arrow : CursorShape.PointingHand;
        TooltipText = target is null ? "" : target.Label + " · " + (Open(target) ? "open" : "secured") + "\nDouble-click to open · right-click for actions";
        QueueRedraw();
    }
    private bool Open(Hotspot target) => target.Region is null
        ? Panel.Simulation.Inventory.DiagnosticItems.Single(i => i.Id == ItemId).GarmentOpen
        : Panel.Simulation.Inventory.IsOpen(ItemId, target.Region);
    public override void _Draw()
    {
        InventoryArt.Draw(this, DefinitionId, Bounds, false);
        if (_hovered is not null) InventoryArt.Highlight(this, DefinitionId, Bounds, _hovered.Points);
    }
    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion) { SetHover(Pick(motion.Position)); return; }
        if (e is not InputEventMouseButton { Pressed: true } mouse) return;
        var spot = Pick(mouse.Position); SetHover(spot);
        if (mouse.ButtonIndex == MouseButton.Right && spot is not null)
            Panel.Context(ItemId, spot.Region, GetGlobalMousePosition(), spot.Region is null);
        else if (mouse.ButtonIndex == MouseButton.Left && mouse.DoubleClick && spot is not null)
        {
            if (spot.Region is not null) Panel.OpenCompartment(ItemId, spot.Region);
            else Panel.OpenGarment(ItemId);
        }
        else if (mouse.ButtonIndex == MouseButton.Left) Panel.Select(ItemId);
        AcceptEvent();
    }
    internal string? HoveredRegion => _hovered?.Region;
    internal bool HasHighlight => _hovered is not null;
    internal Vector2 SourcePosition(Vector2 normalized) => GlobalPosition + InventoryArt.Project(DefinitionId, Bounds, normalized);
    internal Vector2 HotspotPosition(string? region)
    {
        var point = Targets.Single(t => t.Region == region).HitPoint;
        return SourcePosition(new(point[0], point[1]));
    }
}
