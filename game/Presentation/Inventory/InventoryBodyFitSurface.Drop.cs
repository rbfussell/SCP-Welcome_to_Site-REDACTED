using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Body-drop drafts use the same mask/polygon projection as clicks. Release submits ordinary equipment work.</summary>
public partial class InventoryBodyFitSurface
{
    internal BodyFitPreview? DropPreview { get; private set; }
    internal string DropReason { get; private set; } = "Drop clothing onto a body hex.";
    internal bool IsDropTarget => Fit.Panel.DragItemId is not null && GetViewport().GuiGetHoveredControl() == this;
    private BodyHex? PickCell(Vector2 at)
    {
        var nearest = BodyFitGeometry.Cells(Fit.Body.Rows).OrderBy(c => Center(c).DistanceSquaredTo(at)).First();
        var polygon = Enumerable.Range(0, 6).Select(i => Center(nearest)
            + Vector2.FromAngle(Mathf.Pi / 6 + i * Mathf.Pi / 3) * Radius).ToArray();
        return Geometry2D.IsPointInPolygon(at, polygon) ? nearest : null;
    }
    internal void ClearDropPreview() { DropPreview = null; DropReason = "Drop clothing onto a body hex."; QueueRedraw(); }
    internal void PreviewDrop(Vector2 at)
    {
        DropPreview = Fit.Panel.DragItemId is { } id && PickCell(at) is { } cell
            ? Fit.Panel.Simulation.PreviewBodyFitDrop(id, new(cell.Q, cell.R, Fit.Panel.BodyRotation)) : null;
        if (Fit.InspectWorn && DropPreview is { } target)
        {
            var state = Fit.Panel.Simulation.Inventory; var destination = target.Request.Destination!;
            var slot = state.Region(destination.ParentId, destination.RegionId).MountSlot;
            var side = state.Definition(target.Request.ItemId).Wear!.Mounts.Single(m => m.Slot == slot).BodySurface;
            if (side is not null && side != Fit.ViewSurface)
                DropPreview = target with { Handling = target.Handling with { Allowed = false, Reason = $"This item targets the {side} mount; flip to that side first." } };
        }
        DropReason = DropPreview is { } preview ? !preview.Geometry.Fits ? preview.Geometry.Reason : preview.Handling.Reason
            : PickCell(at) is null ? "Drop onto a body hex; empty space is not a fitting anchor."
            : "This item is not currently available for body fitting.";
        if (DropPreview is { } quote) Fit.Panel.ShowQuote(quote.Handling);
        QueueRedraw();
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Fit.Panel.DragItemId) return false;
        PreviewDrop(atPosition);
        return DropPreview is { Geometry.Fits: true, Handling.Allowed: true };
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Fit.Panel.DragItemId) return;
        PreviewDrop(atPosition);
        if (DropPreview is not { Geometry.Fits: true, Handling.Allowed: true } preview) return;
        Fit.Panel.Simulation.HandleInventory(preview.Request); Fit.Panel.ShowReceipt();
    }
}
