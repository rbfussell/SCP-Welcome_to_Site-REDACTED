namespace SiteSimulation;

/// <summary>Required contacts are tested in the same body/surface frame as clothing. An occupied covered contact invalidates the proposed graph.</summary>
internal static class InventoryAttachmentExposure
{
    internal static InventoryAccess Check(InventoryState state, string owner, StorageRegion region, IEnumerable<BodyHex> contacts)
    {
        if (InventoryAttachments.BodyPosition(state, owner) is not { } pose) return new(true, "Provider is not worn.");
        var ancestor = state.Items[owner];
        for (var depth = 0; depth <= InventoryHierarchy.MaxDepth && InventoryWear.Mount(state, ancestor, ancestor.Placement) is null; depth++)
        {
            if (!state.Items.TryGetValue(ancestor.Placement.ParentId, out ancestor!)) return new(false, "No worn attachment provider.");
        }
        var mount = InventoryWear.Mount(state, ancestor, ancestor.Placement);
        if (mount is null) return new(false, "Attachment provider chain exceeds its supported depth.");
        var side = InventoryAttachmentFrames.Provider(state, owner, region.Attachment!.Surface).Surface;
        var points = contacts.Select(c => new BodySurfaceCell(side, BodyFitGeometry.Transform(c, pose))).ToHashSet();
        var blocker = InventoryWear.Worn(state, ancestor.Placement.ParentId).Where(i => InventoryWornOrder.Order(i) > InventoryWornOrder.Order(ancestor))
            .OrderByDescending(InventoryWornOrder.Order).FirstOrDefault(i => InventoryWornStack.Cells(state, i, true).Any(points.Contains));
        return blocker is null ? new(true, "Mounting contacts are exposed.")
            : new(false, "Mounting contact covered by " + state.Definition(blocker.Id).Name + "; move the attachment or expose its support first.");
    }
}
