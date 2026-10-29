namespace SiteSimulation;

/// <summary>Checks the whole exterior assembly, including cousins on separate hooks or worn providers.</summary>
internal static class InventoryAttachmentClearance
{
    private sealed record Part(InventoryItem Item, AttachmentFrame Frame, HashSet<BodyHex> Occupancy, HashSet<BodyHex> Contacts);

    internal static void Validate(InventoryState state)
    {
        var parts = state.Items.Values.Where(i => i.Placement.AttachmentPose is not null).Select(item =>
        {
            var p = item.Placement;
            var support = state.Region(p.ParentId, p.RegionId).Attachment!;
            var provider = InventoryAttachmentFrames.Provider(state, p.ParentId, support.Surface);
            var frame = provider with { Pose = InventoryAttachmentFrames.Compose(provider.Pose, p.AttachmentPose!) };
            var profile = state.Definition(item.Id).Attachment!;
            return new Part(item, frame,
                profile.Occupancy.Select(c => BodyFitGeometry.Transform(c, frame.Pose)).ToHashSet(),
                profile.Contacts.Select(c => BodyFitGeometry.Transform(c, frame.Pose)).ToHashSet());
        }).OrderBy(p => p.Item.Id, StringComparer.Ordinal).ToArray();
        for (var a = 0; a < parts.Length; a++)
        for (var b = a + 1; b < parts.Length; b++)
        {
            var left = parts[a]; var right = parts[b];
            if (left.Frame.AnchorId != right.Frame.AnchorId || left.Frame.Surface != right.Frame.Surface) continue;
            var overlap = left.Occupancy.Intersect(right.Occupancy).ToArray();
            if (overlap.Length == 0) continue;
            // A connector may meet its immediate provider at its own required contacts, never along its whole silhouette.
            if (left.Item.Placement.ParentId == right.Item.Id && overlap.All(left.Contacts.Contains)
                || right.Item.Placement.ParentId == left.Item.Id && overlap.All(right.Contacts.Contains)) continue;
            throw new ArgumentException("Attachment clearance: " + state.Definition(left.Item.Id).Name + " overlaps "
                + state.Definition(right.Item.Id).Name + " on the same exterior surface.");
        }
    }
}
