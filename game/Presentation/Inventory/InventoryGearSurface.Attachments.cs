using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Mounted exterior objects share the physical gear view; they are never loose contents.</summary>
public partial class InventoryGearSurface
{
    private string? _hoveredAttachment;
    private StorageRegion? Support => Panel.Simulation.Inventory.Definition(ItemId).Regions?.SingleOrDefault(r => r.Attachment is not null);
    internal bool HasAttachmentSurface => Support is not null;
    internal AttachmentPreview? MountPreview { get; private set; }
    private Vector2 MountOrigin => Size / 2;
    private const float MountStep = 44;
    private Vector2 MountPoint(BodyHex cell) => MountOrigin + new Vector2(cell.Q + cell.R * .5f, cell.R * .8660254f) * MountStep;
    internal Vector2 MountPosition(BodyHex cell) => GlobalPosition + MountPoint(cell);
    private BodyHex MountCell(Vector2 at) => Support!.Attachment!.Cells.OrderBy(c => MountPoint(c).DistanceSquaredTo(at)).First();
    private const float MountRadius = MountStep / 1.7320508f;
    private IReadOnlyList<InventoryItem> Mounted => Support is { } s ? Panel.Simulation.InventoryAttachmentsOn(ItemId, s.Id) : [];
    private InventoryItem? PickMounted(Vector2 at) => Mounted.LastOrDefault(item =>
    {
        var pose = item.Placement.AttachmentPose!;
        var local = at - MountPoint(new(pose.Q, pose.R));
        return InventoryArt.Opaque(item.DefinitionId, InventoryArt.UnprojectMount(item.DefinitionId, pose.Rotation, local));
    });
    private bool OverSupport(Vector2 at) => Support is not null && MountPoint(MountCell(at)).DistanceTo(at) <= MountRadius;
    private AttachmentPreview PreviewMount(string id, Vector2 at)
    {
        var cell = MountCell(at);
        var preview = Panel.Simulation.PreviewAttachment(id, ItemId, Support!.Id, new(cell.Q, cell.R, Panel.AttachmentRotation));
        if (!OverSupport(at)) preview = preview with { Handling = preview.Handling with { Allowed = false, Reason = "Place the mounting loop on the belt webbing." } };
        return preview;
    }
    private void DrawMounted(string definition, BodyPose pose, float opacity, bool highlight = false)
    {
        InventoryArt.DrawMount(this, definition, MountPoint(new(pose.Q, pose.R)), pose.Rotation, opacity, highlight);
    }
    internal Vector2 MountedImagePoint(string definition, BodyPose pose, Vector2 source)
        => GlobalPosition + MountPoint(new(pose.Q, pose.R)) + InventoryArt.ProjectMount(definition, pose.Rotation, source);
    private void DrawAttachments()
    {
        foreach (var child in Mounted) DrawMounted(child.DefinitionId, child.Placement.AttachmentPose!, Panel.DragItemId == child.Id ? .3f : 1, _hoveredAttachment == child.Id);
        if (MountPreview is not { } preview || Panel.DragItemId is not { } id) return;
        var pose = preview.Request.Destination!.AttachmentPose!;
        DrawMounted(Panel.Simulation.Inventory.Definition(id).Id, pose, .75f);
        var color = new Color(preview.Handling.Allowed ? "69d89a" : "ef8275");
        foreach (var cell in Panel.Simulation.Inventory.Definition(id).Attachment?.Occupancy ?? [])
        {
            var center = MountPoint(BodyFitGeometry.Transform(cell, pose));
            var points = Enumerable.Range(0, 6).Select(i => center + Vector2.FromAngle(i * Mathf.Pi / 3 + Mathf.Pi / 6) * MountRadius).ToArray();
            DrawPolyline(points.Append(points[0]).ToArray(), color, 1);
        }
        DrawString(ThemeDB.FallbackFont, new(8, Size.Y - 8), $"Mount · {pose.Rotation * 60}° · R rotates", fontSize: 13, modulate: color);
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
        if (PickMounted(atPosition) is not { } item) return default;
        var access = Panel.Simulation.InventoryMoveAccess(item.Id);
        if (!access.Allowed) { TooltipText = access.Reason; return default; }
        Panel.Select(item.Id); Panel.BeginDrag(item.Id);
        SetDragPreview(new InventoryDragGhost { Panel = Panel, ItemId = item.Id, Size = new(76, 92), MouseFilter = MouseFilterEnum.Ignore });
        return item.Id;
    }
}
