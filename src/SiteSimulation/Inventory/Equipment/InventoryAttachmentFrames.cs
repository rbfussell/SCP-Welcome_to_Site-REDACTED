namespace SiteSimulation;

/// <summary>A shared exterior plane: worn providers share the actor's surface; loose assemblies keep their own frame.</summary>
internal sealed record AttachmentFrame(string AnchorId, string Surface, BodyPose Pose);

/// <summary>Composes physical support coordinates without creating inventory capacity or a second item parent.</summary>
public static class InventoryAttachmentFrames
{
    public static BodyPose Compose(BodyPose outer, BodyPose inner)
    {
        var cell = BodyFitGeometry.Transform(new(inner.Q, inner.R), outer);
        return new(cell.Q, cell.R, (outer.Rotation + inner.Rotation) % 6);
    }

    /// <summary>Returns a descendant's pose in an ancestor's exterior frame; packed contents have no such pose.</summary>
    public static BodyPose? RelativePose(InventoryState state, string descendant, string ancestor)
    {
        var pose = new BodyPose(0, 0);
        for (var depth = 0; depth <= InventoryHierarchy.MaxDepth; depth++)
        {
            if (descendant == ancestor) return pose;
            if (!state.Items.TryGetValue(descendant, out var item) || item.Placement.AttachmentPose is not { } local) return null;
            pose = Compose(local, pose); descendant = item.Placement.ParentId;
        }
        return null;
    }

    internal static AttachmentFrame Provider(InventoryState state, string owner, string surface)
    {
        var pose = new BodyPose(0, 0);
        var top = state.Items[owner];
        for (var depth = 0; top.Placement.AttachmentPose is { } local; depth++)
        {
            if (depth >= InventoryHierarchy.MaxDepth) throw new ArgumentException("Attachment provider chain is cyclic or too deep.");
            pose = Compose(local, pose);
            var offered = state.Region(top.Placement.ParentId, top.Placement.RegionId).Attachment!;
            if (surface == "mounted") surface = offered.Surface;
            top = state.Items[top.Placement.ParentId];
        }
        var mount = InventoryWear.Mount(state, top, top.Placement);
        if (surface == "mounted") surface = mount?.BodySurface ?? "front";
        return mount is null ? new(top.Id, surface, pose)
            : new(top.Placement.ParentId, surface, Compose(InventoryBodyFit.Pose(state, top)!, pose));
    }
}
