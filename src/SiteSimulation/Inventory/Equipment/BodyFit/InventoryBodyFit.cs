namespace SiteSimulation;

/// <summary>Bridge from geometric fit to the existing equipment graph. No manipulation is performed by previews.</summary>
public static class InventoryBodyFit
{
    public static BodyFitShape? Shape(InventoryState state, string itemId) => state.Definition(itemId).Wear?.FitProfile is { } id
        ? state.BodyFits!.Shape(id) : null;
    public static BodyPose? Pose(InventoryState state, InventoryItem item) => item.Placement.BodyPose ?? Shape(state, item.Id)?.DefaultPose;
    internal static bool SamePlace(InventoryState state, InventoryItem item, ItemPlacement destination)
        => item.Placement with { BodyPose = null, WornOrder = null } == destination with { BodyPose = null, WornOrder = null }
            && (item.Placement.BodyPose ?? Shape(state, item.Id)?.DefaultPose) == (destination.BodyPose ?? Shape(state, item.Id)?.DefaultPose);
    internal static void Validate(InventoryState state, InventoryItem item)
    {
        var p = item.Placement;
        var root = state.Content!.Roots.SingleOrDefault(r => r.Id == p.ParentId && r.ActorId is not null);
        var mount = state.Region(p.ParentId, p.RegionId).MountSlot;
        if (root is null || mount is null or "hand")
        {
            if (p.BodyPose is not null) throw new ArgumentException("Body placement is valid only on worn equipment.");
            return;
        }
        var shape = Shape(state, item.Id);
        if (shape is null)
        {
            if (p.BodyPose is not null) throw new ArgumentException("This equipment has no authored body fit.");
            return;
        }
        if (p.BodyPose is not null && p.Rotated) throw new ArgumentException("Body rotation is separate from storage rotation.");
        if (root.BodyFamily != shape.BodyFamily) throw new ArgumentException("Equipment does not match this person's body family.");
        var result = BodyFitGeometry.Evaluate(state.BodyFits!.Body(root.BodyFamily), shape, p.BodyPose ?? shape.DefaultPose);
        if (!result.Fits) throw new ArgumentException(state.Definition(item.Id).Name + ": " + result.Reason);
    }
}

public sealed partial class SpatialSimulation
{
    public IReadOnlyList<InventoryItem> BodyFitItems
    {
        get
        {
            var root = Inventory.Content?.Roots.SingleOrDefault(r => r.ActorId == Player.Id);
            return root is null ? [] : Inventory.DiagnosticItems.Where(i => i.Placement.ParentId == root.Id
                && Inventory.Definition(i.Id).Wear?.FitProfile is not null).ToArray();
        }
    }
    public BodyFitPreview? PreviewBodyFit(string itemId, BodyPose pose, string? mount = null)
    {
        if (!BodyFitItems.Any(i => i.Id == itemId) || InventoryBodyFit.Shape(Inventory, itemId) is not { } shape) return null;
        var root = Inventory.Content!.Roots.Single(r => r.ActorId == Player.Id);
        if (root.BodyFamily is null) return null;
        var item = Inventory.DiagnosticItems.Single(i => i.Id == itemId);
        var currentMount = item.Placement.ParentId == root.Id ? Inventory.Region(root.Id, item.Placement.RegionId).MountSlot : null;
        var target = mount ?? (currentMount is not null && InventoryWear.Accepts(Inventory.Definition(itemId), currentMount)
            ? item.Placement.RegionId : Inventory.Definition(itemId).WearSlot!);
        var request = new InventoryRequest(InventoryActionKind.Move, itemId, new(root.Id, target, BodyPose: pose));
        return new(BodyFitGeometry.Evaluate(Inventory.BodyFits!.Body(root.BodyFamily), shape, pose), QuoteInventory(request), request);
    }
}
