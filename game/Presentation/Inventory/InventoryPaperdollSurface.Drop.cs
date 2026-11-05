using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Sprite targets quote and submit ordinary actions; drafts never mutate the equipment graph.</summary>
public partial class InventoryPaperdollSurface
{
    internal PaperdollDrop? DropPreview { get; private set; }
    internal PaperdollStorageTarget? StorageTarget { get; private set; }
    internal InventoryPackingQuote? StoragePreview { get; private set; }
    internal bool IsDropTarget => Proof.Panel.DragItemId is not null && GetViewport().GuiGetHoveredControl() == this;
    internal void ClearDrop() { DropPreview = null; StorageTarget = null; StoragePreview = null; QueueRedraw(); }
    internal void PreviewDrop(Vector2 at)
    {
        var panel = Proof.Panel;
        DropPreview = null; StorageTarget = null; StoragePreview = null;
        if (panel.DragItemId is not { } id) return;
        if (panel.Simulation.Inventory.Definition(id).Attachment is not null)
            DropPreview = Proof.Art.AttachmentDrop(panel.Simulation, Proof.Side, Size, at, id, panel.AttachmentRotation);
        if (DropPreview is null) PreviewStorage(id, at);
        if (StorageTarget is not null) { QueueRedraw(); return; }
        DropPreview ??= Proof.Art.BodyDrop(panel.Simulation, Proof.Side, Size, at, id, panel.BodyRotation);
        TooltipText = DropPreview is { } drop ? drop.Quote.Reason : "Choose an exposed compatible mount or body hex; R rotates.";
        if (DropPreview is { } preview)
        { panel.ShowQuote(preview.Quote); if (preview.Geometry is not null && preview.Layers.Length == 0) TooltipText += "\nFit footprint only; this pose has no registered garment sprite."; }
        QueueRedraw();
    }
    /// <summary>A deliberate container target uses normal scoped packing; refusals never become a different action.</summary>
    private void PreviewStorage(string id, Vector2 at)
    {
        StorageTarget = Proof.Art.StorageTarget(Proof.Panel.Simulation, Proof.Side, Size, at);
        if (StorageTarget is { } target)
        {
            StoragePreview = Proof.Panel.PreviewContainerDrop(id, target.Owner, target.Region);
            TooltipText = "Store in " + Proof.Panel.Simulation.Inventory.Name(target.Owner) + " / "
                + Proof.Panel.Simulation.Inventory.Region(target.Owner, target.Region).Name + "\n" + StoragePreview.Reason;
        }
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Proof.Panel.DragItemId) return false;
        PreviewDrop(atPosition); return DropPreview is { Quote.Allowed: true } || StoragePreview is { Allowed: true };
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String || data.AsString() != Proof.Panel.DragItemId) return;
        PreviewDrop(atPosition);
        if (StoragePreview is { Allowed: true } && StorageTarget is { } target)
        { Proof.Panel.StoreInContainer(data.AsString(), target.Owner, target.Region); return; }
        if (DropPreview is not { Quote.Allowed: true } preview) return;
        Proof.Panel.Simulation.HandleInventory(preview.Request); Proof.Panel.ShowReceipt();
    }
}
