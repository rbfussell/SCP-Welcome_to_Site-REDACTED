namespace SiteSimulation;

/// <summary>Hexes are local to the support item, independent of square packing and the worn body pose.</summary>
public sealed record AttachmentSurface(string Interface, BodyHex[] Cells);
public sealed record AttachmentProfile(string Interface, BodyHex[] Occupancy, BodyHex[] Contacts);
public sealed record AttachmentPreview(InventoryActionQuote Handling, InventoryRequest Request);

public static class InventoryAttachments
{
    private static bool CellsValid(BodyHex[]? cells) => cells is { Length: > 0 and <= 128 }
        && cells.Distinct().Count() == cells.Length && cells.All(c => c.Q is >= -32 and <= 32 && c.R is >= -32 and <= 32);
    private static bool InterfaceValid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 64;
    internal static bool ProfileValid(AttachmentProfile? profile) => profile is null || InterfaceValid(profile.Interface)
        && CellsValid(profile.Occupancy) && CellsValid(profile.Contacts) && profile.Contacts.All(profile.Occupancy.Contains);
    internal static bool SurfaceValid(StorageRegion region) => region.Attachment is not { } surface
        || InterfaceValid(surface.Interface) && CellsValid(surface.Cells) && region.MountSlot is null && region.Mask is null
            && region.AccessFootprint is null;

    public static BodyHex[] Occupancy(InventoryState state, InventoryItem item) => state.Definition(item.Id).Attachment is { } profile
        && item.Placement.AttachmentPose is { } pose ? profile.Occupancy.Select(c => BodyFitGeometry.Transform(c, pose)).ToArray() : [];

    internal static void Validate(InventoryState state, InventoryItem item)
    {
        var p = item.Placement;
        if (state.Region(p.ParentId, p.RegionId).Attachment is not { } support)
        {
            if (p.AttachmentPose is not null) throw new ArgumentException("Attachment position requires a support surface.");
            return;
        }
        if (!state.Items.ContainsKey(p.ParentId) || state.Definition(item.Id).Attachment is not { } profile || profile.Interface != support.Interface)
            throw new ArgumentException("Item has no compatible attachment interface.");
        if (p.AttachmentPose is not { Q: >= -64 and <= 64, R: >= -64 and <= 64, Rotation: >= 0 and <= 5 } pose
            || p.X != 0 || p.Y != 0 || p.Rotated || p.BodyPose is not null)
            throw new ArgumentException("Choose a six-direction position on the support, separate from storage/body coordinates.");
        if (profile.Contacts.Select(c => BodyFitGeometry.Transform(c, pose)).Any(c => !support.Cells.Contains(c)))
            throw new ArgumentException("Attachment contacts must sit on the support surface.");
        var occupied = Occupancy(state, item).ToHashSet();
        if (state.Children(p.ParentId, p.RegionId).Any(other => other.Id != item.Id && Occupancy(state, other).Any(occupied.Contains)))
            throw new ArgumentException("Attachment overlaps equipment already mounted on this surface.");
    }

    /// <summary>Resolve through every physical support; parent movement carries local coordinates without rewriting children.</summary>
    public static BodyPose? BodyPosition(InventoryState state, string id)
    {
        var item = state.DiagnosticItems.Single(i => i.Id == id);
        if (item.Placement.AttachmentPose is not { } local)
            return InventoryWear.Mount(state, item, item.Placement) is null ? null : InventoryBodyFit.Pose(state, item);
        if (BodyPosition(state, item.Placement.ParentId) is not { } parent) return null;
        var cell = BodyFitGeometry.Transform(new(local.Q, local.R), parent);
        return new(cell.Q, cell.R, (parent.Rotation + local.Rotation) % 6);
    }
}

public sealed partial class SpatialSimulation
{
    public AttachmentPreview PreviewAttachment(string itemId, string owner, string surface, BodyPose pose)
    {
        var request = new InventoryRequest(InventoryActionKind.Move, itemId, new(owner, surface, AttachmentPose: pose));
        return new(QuoteInventory(request), request);
    }
    /// <summary>Container-style exterior drop: choose free support at the requested orientation without moving existing children.</summary>
    public AttachmentPreview PreviewAttachmentDrop(string itemId, string owner, int rotation = 0)
    {
        var fallback = new InventoryRequest(InventoryActionKind.Move, itemId);
        if (!Inventory.Items.TryGetValue(itemId, out var incoming) || !Inventory.Items.ContainsKey(owner) || rotation is < 0 or > 5)
            return new(new(false, "Choose known equipment and a valid attachment orientation.", 0, 0), fallback);
        var profile = Inventory.Definition(itemId).Attachment;
        var supports = Inventory.Definition(owner).Regions?.Where(r => r.Attachment is not null).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray() ?? [];
        if (profile is null || !supports.Any(s => s.Attachment!.Interface == profile.Interface))
            return new(new(false, "No compatible exterior attachment surface.", 0, 0), fallback);
        if (incoming.Placement.ParentId == owner && incoming.Placement.AttachmentPose is not null)
            return new(new(false, "Already attached here; reposition it on the gear illustration.", 0, 0), fallback);
        AttachmentPreview? refused = null;
        // A contact need not sit at the authored origin: enumerate every support/contact alignment, not just origins on support.
        foreach (var support in supports.Where(s => s.Attachment!.Interface == profile.Interface))
        {
            var origins = support.Attachment!.Cells.SelectMany(cell => profile.Contacts.Select(contact =>
            {
                var rotated = contact.Rotate(rotation); return new BodyHex(cell.Q - rotated.Q, cell.R - rotated.R);
            })).Distinct().OrderBy(c => Math.Max(Math.Abs(c.Q), Math.Max(Math.Abs(c.R), Math.Abs(c.Q + c.R))))
                .ThenBy(c => c.R).ThenBy(c => c.Q);
            foreach (var cell in origins)
            {
                var preview = PreviewAttachment(itemId, owner, support.Id, new(cell.Q, cell.R, rotation));
                if (preview.Handling.Allowed) return preview;
                refused ??= preview;
            }
        }
        return refused ?? new(new(false, "No free compatible support; existing attachments stay in place.", 0, 0), fallback);
    }
    public IReadOnlyList<InventoryItem> InventoryAttachmentsOn(string owner, string surface)
        => InventoryAccess(owner, surface).Allowed ? Inventory.Children(owner, surface) : [];
}
