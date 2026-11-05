using Godot;
using System.Text.Json;
using SiteSimulation;

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
    internal static bool RegisteredRegion(string definition, string region) =>
        Bindings.GetValueOrDefault(definition)?.Any(target => target.Region == region) == true;
    private string DefinitionId => Panel.Simulation.Inventory.Definition(ItemId).Id;
    private Hotspot[] Targets => Bindings.GetValueOrDefault(DefinitionId) ?? [];
    private Rect2 Bounds => new(Vector2.Zero, Size);
    private Hotspot? _hovered;
    private bool? _dropAllowed;
    private PocketCoverage? _coverage;
    public override void _Ready()
    {
        CustomMinimumSize = new(330, 350); MouseFilter = MouseFilterEnum.Stop; TextureFilter = TextureFilterEnum.Nearest;
        MouseExited += () => { _dropAllowed = null; MountPreview = null; _hoveredAttachment = null; SetHover(null); };
        Resized += () => { SetHover(null); QueueRedraw(); };
    }
    private Hotspot? Pick(Vector2 position)
    {
        var source = MountedProviderArt ? InventoryArt.UnprojectMount(DefinitionId, 0, position - MountOrigin)
            : InventoryArt.Unproject(DefinitionId, Bounds, position);
        if (!InventoryArt.Opaque(DefinitionId, source)) return null;
        // Later entries win where a small visible feature lies over a larger segment.
        return Targets.LastOrDefault(t => Geometry2D.IsPointInPolygon(source, t.Points));
    }
    private void SetHover(Hotspot? target)
    {
        if (!IsInsideTree()) return;
        _hovered = target;
        _coverage = target?.Region is { } region ? Panel.Simulation.InventoryPocketCoverage(ItemId, region) : null;
        MouseDefaultCursorShape = target is null ? CursorShape.Arrow : CursorShape.PointingHand;
        TooltipText = target?.Region is { } support && Panel.Simulation.Inventory.Region(ItemId, support).Attachment is not null
            ? "Exterior support · drag compatible gear onto its mount point · R rotates its mounting pose"
            : target is null ? "" : target.Label + " · " + (target.Region is { } id && !Panel.Simulation.Inventory.Region(ItemId, id).Sealable ? "unsealable" + (Panel.Simulation.Inventory.Region(ItemId, id).Snug ? " · snug" : "") : Open(target) ? "open" : "secured") + (_coverage?.Covered == true ? "\n" + _coverage.Reason : "\nDouble-click to open · right-click for actions");
        QueueRedraw();
    }
    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        if (_hovered is not null) SetHover(_hovered);
        if (HasAttachmentSurface) HoverAttachment(GetLocalMousePosition());
        if (MountPreview is not null && Panel.DragItemId is { } id && GetViewport().GuiIsDragging())
        {
            MountPreview = PreviewMount(id, GetLocalMousePosition());
            Panel.ShowQuote(MountPreview.Handling); QueueRedraw();
        }
    }
    private bool Open(Hotspot target) => target.Region is null
        ? Panel.Simulation.Inventory.DiagnosticItems.Single(i => i.Id == ItemId).GarmentOpen
        : Panel.Simulation.Inventory.IsOpen(ItemId, target.Region);
    public override void _Draw()
    {
        if (MountedProviderArt) DrawMounted(DefinitionId, new(0, 0), 1, _hovered is not null);
        else
        {
            InventoryArt.Draw(this, DefinitionId, Bounds, false);
            if (_hovered is not null) InventoryArt.Highlight(this, DefinitionId, Bounds, _hovered.Points, _coverage?.Covered == true);
        }
        DrawAttachments();
        if (!HasAttachmentSurface && _dropAllowed is { } allowed)
            DrawString(ThemeDB.FallbackFont, new(8, Size.Y - 8), allowed ? "Store in this compartment" : "Cannot store here", fontSize: 13,
                modulate: new(allowed ? "69d89a" : "ef8275"));
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Panel.DragItemId) return false;
        if (HasAttachmentSurface)
        {
            MountPreview = PreviewMount(data.AsString(), atPosition); Panel.ShowQuote(MountPreview.Handling); QueueRedraw();
            return MountPreview.Handling.Allowed;
        }
        var target = Pick(atPosition); SetHover(target);
        _dropAllowed = target?.Region is { } region && Panel.PreviewContainerDrop(data.AsString(), ItemId, region).Allowed;
        return _dropAllowed.Value;
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (HasAttachmentSurface)
        { var preview = PreviewMount(data.AsString(), atPosition); if (preview.Handling.Allowed) Panel.Simulation.HandleInventory(preview.Request); Panel.ShowReceipt(); MountPreview = null; QueueRedraw(); return; }
        if (Pick(atPosition)?.Region is { } region) Panel.StoreInContainer(data.AsString(), ItemId, region);
        _dropAllowed = null; QueueRedraw();
    }
    public override void _Notification(int what)
    { if (what == NotificationDragEnd) { _dropAllowed = null; MountPreview = null; _pressedAttachment = null; QueueRedraw(); } }
    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        { SetHover(Pick(motion.Position)); HoverAttachment(motion.Position); return; }
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) _pressedAttachment = null;
        if (e is not InputEventMouseButton { Pressed: true } mouse) return;
        var mounted = PickMounted(mouse.Position);
        _pressedAttachment = mouse.ButtonIndex == MouseButton.Left ? mounted?.Id : null;
        if (mounted is not null)
        {
            if (mouse.ButtonIndex == MouseButton.Right) Panel.Context(mounted.Id, null, GetGlobalMousePosition());
            else if (mouse.ButtonIndex == MouseButton.Left && mouse.DoubleClick) Panel.Inspect(mounted.Id);
            else if (mouse.ButtonIndex == MouseButton.Left) Panel.Select(mounted.Id);
            AcceptEvent(); return;
        }
        if (HasAttachmentSurface && mouse.ButtonIndex == MouseButton.Left && !mouse.DoubleClick && Panel.SelectedItem is { } selected && selected != ItemId && OverSupport(mouse.Position))
        { var preview = PreviewMount(selected, mouse.Position); Panel.Simulation.HandleInventory(preview.Request); Panel.ShowReceipt(); AcceptEvent(); return; }
        var spot = Pick(mouse.Position); SetHover(spot);
        if (HasAttachmentSurface)
        {
            if (spot is not null && mouse.ButtonIndex == MouseButton.Right) Panel.Context(ItemId, null, GetGlobalMousePosition());
            else if (mouse.ButtonIndex == MouseButton.Left) Panel.Select(ItemId);
            AcceptEvent(); return;
        }
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
    internal bool HoverCovered => _coverage?.Covered == true;
    internal bool HasHighlight => _hovered is not null;
    internal Vector2 SourcePosition(Vector2 normalized) => MountedProviderArt ? MountedImagePoint(DefinitionId, new(0, 0), normalized)
        : GlobalPosition + InventoryArt.Project(DefinitionId, Bounds, normalized);
    internal Vector2 HotspotPosition(string? region)
    {
        var point = Targets.Single(t => t.Region == region).HitPoint;
        return SourcePosition(new(point[0], point[1]));
    }
}
