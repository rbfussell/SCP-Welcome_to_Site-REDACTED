using Godot;

namespace SitePresentation;

/// <summary>Sprite targets quote and submit ordinary actions; drafts never mutate the equipment graph.</summary>
public partial class InventoryPaperdollSurface
{
    internal PaperdollDrop? DropPreview { get; private set; }
    internal bool IsDropTarget => Proof.Panel.DragItemId is not null && GetViewport().GuiGetHoveredControl() == this;
    internal void ClearDrop() { DropPreview = null; QueueRedraw(); }
    internal void PreviewDrop(Vector2 at)
    {
        var panel = Proof.Panel;
        DropPreview = null;
        if (panel.DragItemId is not { } id) return;
        if (panel.Simulation.Inventory.Definition(id).Attachment is not null)
            DropPreview = Proof.Art.AttachmentDrop(panel.Simulation, Proof.Side, Size, at, id, panel.AttachmentRotation);
        DropPreview ??= Proof.Art.BodyDrop(panel.Simulation, Proof.Side, Size, at, id, panel.BodyRotation);
        TooltipText = DropPreview is { } drop ? drop.Quote.Reason : "Choose an exposed compatible mount or body hex; R rotates.";
        if (DropPreview is { } preview)
        { panel.ShowQuote(preview.Quote); if (preview.Geometry is not null && preview.Layers.Length == 0) TooltipText += "\nFit footprint only; this pose has no registered garment sprite."; }
        QueueRedraw();
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Proof.Panel.DragItemId) return false;
        PreviewDrop(atPosition); return DropPreview is { Quote.Allowed: true };
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Proof.Panel.DragItemId) return;
        PreviewDrop(atPosition);
        if (DropPreview is not { Quote.Allowed: true } preview) return;
        Proof.Panel.Simulation.HandleInventory(preview.Request); Proof.Panel.ShowReceipt();
    }
}
