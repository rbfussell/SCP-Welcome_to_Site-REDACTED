using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Exterior assemblies share one physical frame for drawing, picking and precise mount targets.</summary>
public partial class InventoryGearSurface
{
    private sealed record MountedPart(InventoryItem Item, BodyPose Pose);
    private sealed record MountTarget(string Owner, StorageRegion Region, BodyHex Cell, BodyPose Provider, float Distance);
    private string? _hoveredAttachment;
    private string? _pressedAttachment;
    internal bool HasAttachmentSurface => Panel.Simulation.Inventory.Definition(ItemId).Regions?.Any(r => r.Attachment is not null) == true;
    private bool MountedProviderArt => HasAttachmentSurface && InventoryArt.HasMountIllustration(DefinitionId);
    internal AttachmentPreview? MountPreview { get; private set; }
    private Vector2 MountOrigin => Size / 2;
    private const float MountStep = 44;
    private const float MountRadius = MountStep / 1.7320508f;
    private Vector2 MountPoint(BodyHex cell) => MountOrigin + new Vector2(cell.Q + cell.R * .5f, cell.R * .8660254f) * MountStep;
    internal Vector2 MountPosition(BodyHex cell) => GlobalPosition + MountPoint(cell);

    private IEnumerable<MountedPart> Descendants(string owner, BodyPose pose, int depth = 0)
    {
        if (depth >= InventoryHierarchy.MaxDepth) yield break;
        foreach (var region in (Panel.Simulation.Inventory.Definition(owner).Regions ?? []).Where(r => r.Attachment is not null))
        foreach (var item in Panel.Simulation.InventoryAttachmentsOn(owner, region.Id))
        {
            var resolved = InventoryAttachmentFrames.Compose(pose, item.Placement.AttachmentPose!);
            yield return new(item, resolved);
            foreach (var child in Descendants(item.Id, resolved, depth + 1)) yield return child;
        }
    }
    private MountedPart[] Mounted => Descendants(ItemId, new(0, 0)).ToArray();
    private InventoryItem? PickMounted(Vector2 at) => Mounted.LastOrDefault(part =>
        InventoryArt.Opaque(part.Item.DefinitionId, InventoryArt.UnprojectMount(part.Item.DefinitionId, part.Pose.Rotation,
            at - MountPoint(new(part.Pose.Q, part.Pose.R)))))?.Item;
    internal string? MountedAt(Vector2 global) => PickMounted(global - GlobalPosition)?.Id;

    private MountTarget? NearestMount(Vector2 at)
    {
        var providers = new[] { (Id: ItemId, Pose: new BodyPose(0, 0)) }.Concat(Mounted.Select(p => (Id: p.Item.Id, Pose: p.Pose)));
        return providers.SelectMany(provider => (Panel.Simulation.Inventory.Definition(provider.Id).Regions ?? [])
            .Where(r => r.Attachment is not null && Panel.Simulation.InventoryAccess(provider.Id, r.Id).Allowed)
            .SelectMany(region => region.Attachment!.Cells.Select(cell => new MountTarget(provider.Id, region, cell, provider.Pose,
                MountPoint(BodyFitGeometry.Transform(cell, provider.Pose)).DistanceTo(at)))))
            .OrderBy(t => t.Distance).ThenBy(t => t.Owner, StringComparer.Ordinal).ThenBy(t => t.Region.Id, StringComparer.Ordinal).FirstOrDefault();
    }
    private bool OverSupport(Vector2 at) => NearestMount(at) is { Distance: <= MountRadius };
    private AttachmentPreview PreviewMount(string id, Vector2 at)
    {
        var target = NearestMount(at);
        var preview = Panel.Simulation.PreviewAttachment(id, target?.Owner ?? ItemId, target?.Region.Id ?? "missing",
            new(target?.Cell.Q ?? 0, target?.Cell.R ?? 0, Panel.AttachmentRotation));
        if (target is null || target.Distance > MountRadius)
            preview = preview with { Handling = preview.Handling with { Allowed = false, Reason = "Place the connector on an exposed compatible mount point." } };
        return preview;
    }
    private void DrawMounted(string definition, BodyPose pose, float opacity, bool highlight = false)
        => InventoryArt.DrawMount(this, definition, MountPoint(new(pose.Q, pose.R)), pose.Rotation, opacity, highlight);
    internal Vector2 MountedImagePoint(string definition, BodyPose pose, Vector2 source)
        => GlobalPosition + MountPoint(new(pose.Q, pose.R)) + InventoryArt.ProjectMount(definition, pose.Rotation, source);
    private void DrawAttachments()
    {
        var state = Panel.Simulation.Inventory;
        foreach (var part in Mounted)
            DrawMounted(part.Item.DefinitionId, part.Pose, Panel.DragItemId is { } moving
                && InventoryAttachmentFrames.RelativePose(state, part.Item.Id, moving) is not null ? .3f : 1, _hoveredAttachment == part.Item.Id);
        if (MountPreview is not { } preview || Panel.DragItemId is not { } id || preview.Request.Destination is not { AttachmentPose: { } local } destination) return;
        var provider = InventoryAttachmentFrames.RelativePose(state, destination.ParentId, ItemId);
        if (provider is null) return;
        var pose = InventoryAttachmentFrames.Compose(provider, local);
        var color = new Color(preview.Handling.Allowed ? "69d89a" : "ef8275");
        var assembly = new[] { new MountedPart(state.DiagnosticItems.Single(i => i.Id == id), pose) }.Concat(Descendants(id, pose));
        foreach (var part in assembly)
        {
            DrawMounted(part.Item.DefinitionId, part.Pose, .75f);
            foreach (var cell in state.Definition(part.Item.Id).Attachment?.Occupancy ?? [])
            {
                var center = MountPoint(BodyFitGeometry.Transform(cell, part.Pose));
                var points = Enumerable.Range(0, 6).Select(i => center + Vector2.FromAngle(i * Mathf.Pi / 3 + Mathf.Pi / 6) * MountRadius).ToArray();
                DrawPolyline(points.Append(points[0]).ToArray(), color, 1);
            }
        }
        DrawString(ThemeDB.FallbackFont, new(8, Size.Y - 8), $"{state.Definition(destination.ParentId).Name} · {local.Rotation * 60}° · R rotates", fontSize: 13, modulate: color);
    }
    private void HoverAttachment(Vector2 at)
    {
        _hoveredAttachment = PickMounted(at)?.Id;
        if (_hoveredAttachment is not { } id) return;
        _hovered = null; MouseDefaultCursorShape = CursorShape.PointingHand;
        TooltipText = Panel.Describe(id) + "\nDrag to reposition · double-click to inspect · right-click for actions";
        QueueRedraw();
    }
    public override Variant _GetDragData(Vector2 atPosition)
    {
        // Tiny hardware can leave the pointer before Godot's drag threshold is crossed.
        // Retain the item actually pressed, rather than picking again at the moved pointer.
        var item = Mounted.Select(p => p.Item).FirstOrDefault(i => i.Id == _pressedAttachment) ?? PickMounted(atPosition);
        if (item is null) return default;
        var access = Panel.Simulation.InventoryMoveAccess(item.Id);
        if (!access.Allowed) { TooltipText = access.Reason; return default; }
        Panel.Select(item.Id); Panel.BeginDrag(item.Id);
        SetDragPreview(new InventoryDragGhost { Panel = Panel, ItemId = item.Id, Size = new(76, 92), MouseFilter = MouseFilterEnum.Ignore });
        return item.Id;
    }
}
