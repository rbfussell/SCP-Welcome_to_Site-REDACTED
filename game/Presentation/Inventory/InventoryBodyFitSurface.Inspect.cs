using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Exposed worn-item picking and drag sources, separate from fitting drafts and future alpha artwork.</summary>
public partial class InventoryBodyFitSurface
{
    private string? _hoveredItem, _pressedItem;
    private BodyHex? _pressedCell;
    internal string? HoveredItem => _hoveredItem;
    internal void ClearInspection() { _hoveredItem = _pressedItem = null; _pressedCell = null; TooltipText = ""; MouseDefaultCursorShape = CursorShape.Arrow; QueueRedraw(); }
    private BodyEquipmentTarget? PickWorn(Vector2 at) => PickCell(at) is { } cell ? Fit.Panel.Simulation.InventoryBodyPick(Fit.ViewSurface, cell) : null;
    private void HoverWorn(Vector2 at)
    {
        var hit = PickWorn(at); _hoveredItem = hit?.ItemId;
        TooltipText = hit is null ? "No exposed worn gear here." : Fit.Panel.Describe(hit.ItemId) + "\n" + hit.MoveAccess.Reason
            + "\nDouble-click to inspect · right-click for actions · drag to move";
        MouseDefaultCursorShape = hit is null ? CursorShape.Arrow : CursorShape.PointingHand; QueueRedraw();
    }
    public override void _Process(double delta)
    {
        if (IsVisibleInTree() && Fit.InspectWorn && GetViewport().GuiGetHoveredControl() == this && !GetViewport().GuiIsDragging())
            HoverWorn(GetLocalMousePosition());
    }
    private void InspectInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion) { HoverWorn(motion.Position); return; }
        if (e is not InputEventMouseButton mouse) return;
        if (!mouse.Pressed) { _pressedItem = null; _pressedCell = null; return; }
        var hit = PickWorn(mouse.Position);
        if (mouse.ButtonIndex == MouseButton.Right)
        { if (hit is not null) Fit.Panel.Context(hit.ItemId, null, GetGlobalMousePosition()); AcceptEvent(); return; }
        if (mouse.ButtonIndex != MouseButton.Left) return;
        _pressedItem = hit?.ItemId; _pressedCell = PickCell(mouse.Position);
        if (hit is not null)
        {
            Fit.Choose(hit.ItemId);
            if (mouse.DoubleClick) Fit.Panel.Inspect(hit.ItemId);
        }
        AcceptEvent();
    }
    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!Fit.InspectWorn || _pressedItem is not { } id || _pressedCell is not { } cell
            || Fit.Panel.Simulation.InventoryBodyPick(Fit.ViewSurface, cell)?.ItemId != id) return default;
        var access = Fit.Panel.Simulation.InventoryMoveAccess(id);
        if (!access.Allowed) { Fit.Panel.ShowQuote(new(false, access.Reason, 0, 0)); TooltipText = access.Reason; return default; }
        Fit.Panel.Select(id); Fit.Panel.BeginDrag(id);
        SetDragPreview(new InventoryDragGhost { Panel = Fit.Panel, ItemId = id, Size = new(90, 110), MouseFilter = MouseFilterEnum.Ignore });
        return id;
    }
    public override void _Notification(int what)
    { if (what == NotificationDragEnd) { _pressedItem = null; _pressedCell = null; } }
    private void DrawOutfit()
    {
        string[] palette = ["697f90", "8c8271", "687e60", "887159", "7d748a", "a2976a", "708783"];
        foreach (var target in Fit.Panel.Simulation.InventoryBodySurface(Fit.ViewSurface))
        {
            var definition = Fit.Panel.Simulation.Inventory.Definition(target.ItemId);
            var color = new Color(palette[definition.Id.Sum(c => (int)c) % palette.Length]);
            foreach (var cell in target.Cells)
            {
                var polygon = Polygon(cell); DrawColoredPolygon(polygon, new Color(color, Fit.Panel.DragItemId == target.ItemId ? .3f : .85f));
                if (target.ItemId == _hoveredItem || target.ItemId == Fit.SelectedId)
                    DrawPolyline(polygon.Append(polygon[0]).ToArray(), target.ItemId == _hoveredItem ? new("e7e0bd") : new("b6cabc"), 1.4f);
            }
        }
    }
}
