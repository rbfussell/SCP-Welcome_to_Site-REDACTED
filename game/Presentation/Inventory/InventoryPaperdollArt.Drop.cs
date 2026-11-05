using Godot;
using SiteSimulation;

namespace SitePresentation;

internal sealed record PaperdollBodyPlane(float CenterX, float CellWidth, float[] Rows);
internal sealed record PaperdollMountTarget(string Owner, string Region, BodyHex Cell, Vector2 Origin, float Step, int Order);
internal sealed record PaperdollDrop(InventoryRequest Request, InventoryActionQuote Quote, PaperdollLayer[] Layers, Vector2 Anchor, BodyFitResult? Geometry = null);

internal sealed partial class InventoryPaperdollArt
{
    private static void ValidateBodyPlane(PaperdollBodyPlane? plane)
    {
        if (plane is null) return;
        if (!float.IsFinite(plane.CenterX) || plane.CenterX is < 0 or > 1 || !float.IsFinite(plane.CellWidth) || plane.CellWidth is <= 0 or > .2f
            || plane.Rows is not { Length: 20 } || plane.Rows.Any(y => !float.IsFinite(y) || y is < 0 or > 1)
            || !plane.Rows.Zip(plane.Rows.Skip(1)).All(p => p.First < p.Second))
            throw new InvalidOperationException("Invalid reference-body fitting projection.");
    }
    internal Vector2 BodyPoint(BodyHex cell, string side)
    {
        var plane = _manifest.BodyPlane!; var r = Math.Clamp(cell.R, 0, plane.Rows.Length - 1);
        var y = plane.Rows[r] + (cell.R - r) * .05f;
        return new(plane.CenterX + (side == "back" ? -1 : 1) * (cell.Q + cell.R * .5f) * plane.CellWidth, y);
    }
    private Vector2[] BodyPolygon(BodyHex cell, string side)
    {
        var center = BodyPoint(cell, side); var r = Math.Clamp(cell.R, 1, _manifest.BodyPlane!.Rows.Length - 2);
        var height = (_manifest.BodyPlane.Rows[r + 1] - _manifest.BodyPlane.Rows[r - 1]) / 3;
        return Enumerable.Range(0, 6).Select(i => center + new Vector2(Mathf.Cos(Mathf.Pi / 6 + i * Mathf.Pi / 3)
            * _manifest.BodyPlane.CellWidth / 1.7320508f, Mathf.Sin(Mathf.Pi / 6 + i * Mathf.Pi / 3) * height)).ToArray();
    }
    internal PaperdollDrop? BodyDrop(SpatialSimulation sim, string side, Vector2 size, Vector2 at, string id, int rotation)
    {
        if (_manifest.BodyPlane is null || InventoryBodyFit.Shape(sim.Inventory, id) is not { } shape) return null;
        var uv = (at - Bounds(size).Position) / Bounds(size).Size;
        var body = sim.Inventory.BodyFits!.Body(_manifest.BodyFamily);
        var cells = BodyFitGeometry.Cells(body.Rows).Where(c => Geometry2D.IsPointInPolygon(uv, BodyPolygon(c, side))).ToArray();
        if (cells.Length == 0) return null;
        var cell = cells.OrderBy(c => BodyPoint(c, side).DistanceSquaredTo(uv)).First();
        var mount = sim.Inventory.Definition(id).Wear!.Mounts.FirstOrDefault(m => m.BodySurface == side)?.Slot;
        var preview = sim.PreviewBodyFitDrop(id, new(cell.Q, cell.R, rotation), mount);
        if (preview is null) return null;
        var quote = preview.Handling;
        var destination = preview.Request.Destination!;
        var targetSide = sim.Inventory.Definition(id).Wear!.Mounts.Single(m => m.Slot == sim.Inventory.Region(destination.ParentId, destination.RegionId).MountSlot).BodySurface;
        if (targetSide is not null && targetSide != side) quote = quote with { Allowed = false, Reason = "Flip to the item's mount side first." };
        var binding = _manifest.Bindings.SingleOrDefault(b => b.Definition == sim.Inventory.Definition(id).Id);
        var layers = new List<PaperdollLayer>();
        if (binding is not null && destination.BodyPose == shape.DefaultPose)
        {
            var item = sim.Inventory.DiagnosticItems.Single(i => i.Id == id);
            var column = item.GarmentOpen ? binding.OpenColumn ?? binding.Column : binding.Column;
            if (binding.MountColumns is { } mounts && mounts.TryGetValue(mount ?? sim.Inventory.Definition(id).WearSlot!, out var mountedColumn)) column = mountedColumn;
            layers.Add(new(id, column));
        }
        return new(preview.Request, quote, layers.ToArray(), BodyPoint(cell, side), preview.Geometry);
    }
    /// <summary>Only registered, visible providers offer targets. Saved local cells remain the physical authority.</summary>
    internal IEnumerable<PaperdollMountTarget> MountTargets(SpatialSimulation sim, string side)
    {
        var state = sim.Inventory; var layers = Layers(sim);
        foreach (var layer in layers.Where(l => l.ItemId is not null))
        {
            var id = layer.ItemId!; var root = id;
            for (var depth = 0; depth < InventoryHierarchy.MaxDepth; depth++)
            {
                var item = state.DiagnosticItems.Single(i => i.Id == root);
                if (item.Placement.AttachmentPose is null) break;
                root = item.Placement.ParentId;
            }
            var rootItem = state.DiagnosticItems.Single(i => i.Id == root);
            var binding = _manifest.Bindings.SingleOrDefault(b => b.Definition == rootItem.DefinitionId);
            if (binding?.AttachmentPlane is not { } plane) continue;
            var slot = state.Region(rootItem.Placement.ParentId, rootItem.Placement.RegionId).MountSlot;
            var surface = layer.Mounted?.Surface ?? state.Definition(root).Wear!.Mounts.Single(m => m.Slot == slot).BodySurface ?? "front";
            var pose = InventoryAttachmentFrames.RelativePose(state, id, root)!;
            foreach (var region in (state.Definition(id).Regions ?? []).Where(r => r.Attachment is not null))
            {
                var offered = region.Attachment!;
                if ((offered.Surface == "mounted" ? surface : offered.Surface) != side) continue;
                foreach (var cell in offered.Cells)
                {
                    var resolved = BodyFitGeometry.Transform(cell, pose);
                    var uv = new Vector2(plane.Origin[0], plane.Origin[1]) + new Vector2(resolved.Q + resolved.R * .5f, resolved.R * .8660254f)
                        * plane.CellPixels / new Vector2(_manifest.CanvasSize[0], _manifest.CanvasSize[1]);
                    // Existing descendants may occupy this contact: show a refusal rather than hiding the target.
                    var later = layers.SkipWhile(l => l != layer).Skip(1).Where(l => l.ItemId is { } other
                        && InventoryAttachmentFrames.RelativePose(state, other, id) is null);
                    if (HeadOpaque(side, uv) || later.Any(l => Opaque(l, side, uv))) continue;
                    yield return new(id, region.Id, cell, uv, plane.CellPixels, Array.IndexOf(layers, layer));
                }
            }
        }
    }
    internal PaperdollDrop? AttachmentDrop(SpatialSimulation sim, string side, Vector2 size, Vector2 at, string id, int rotation)
    {
        var bounds = Bounds(size);
        var target = MountTargets(sim, side).OrderBy(t => (bounds.Position + t.Origin * bounds.Size).DistanceSquaredTo(at))
            .ThenByDescending(t => t.Order).ThenBy(t => t.Owner, StringComparer.Ordinal).FirstOrDefault();
        if (target is null || (bounds.Position + target.Origin * bounds.Size).DistanceTo(at) > target.Step * bounds.Size.X / _manifest.CanvasSize[0] / 1.7320508f) return null;
        var preview = sim.PreviewAttachment(id, target.Owner, target.Region, new(target.Cell.Q, target.Cell.R, rotation));
        var owner = Layers(sim).Single(l => l.ItemId == target.Owner);
        var inheritedRotation = owner.Mounted?.Rotation ?? 0;
        var art = (_manifest.Attachments ?? []).SingleOrDefault(b => b.Definition == sim.Inventory.Definition(id).Id && b.Surface == side);
        var layers = new List<PaperdollLayer>();
        if (art is not null)
        {
            var baseRotation = (inheritedRotation + rotation) % 6;
            layers.Add(new(id, art.Column, new(target.Origin, new(art.Anchor[0], art.Anchor[1]), baseRotation, side)));
            IEnumerable<PaperdollLayer> Children(string parent, BodyPose parentPose, string inheritedSide, int depth)
            {
                if (depth >= InventoryHierarchy.MaxDepth) yield break;
                foreach (var region in (sim.Inventory.Definition(parent).Regions ?? []).Where(r => r.Attachment is not null))
                foreach (var child in sim.Inventory.Children(parent, region.Id))
                {
                    var childSide = region.Attachment!.Surface == "mounted" ? inheritedSide : region.Attachment.Surface;
                    var childArt = (_manifest.Attachments ?? []).SingleOrDefault(b => b.Definition == child.DefinitionId && b.Surface == childSide);
                    if (childArt is null || child.Placement.AttachmentPose is not { } local) continue;
                    var pose = InventoryAttachmentFrames.Compose(parentPose, local);
                    var uv = target.Origin + new Vector2(pose.Q + pose.R * .5f, pose.R * .8660254f) * target.Step
                        / new Vector2(_manifest.CanvasSize[0], _manifest.CanvasSize[1]);
                    yield return new(child.Id, childArt.Column, new(uv, new(childArt.Anchor[0], childArt.Anchor[1]), pose.Rotation, childSide));
                    foreach (var nested in Children(child.Id, pose, childSide, depth + 1)) yield return nested;
                }
            }
            layers.AddRange(Children(id, new(0, 0, baseRotation), side, 0));
        }
        return new(preview.Request, preview.Handling, layers.ToArray(), target.Origin);
    }
    internal void DrawDrop(Control canvas, string side, PaperdollDrop drop)
    {
        var bounds = Bounds(canvas.Size); var color = drop.Quote.Allowed ? new Color(.5f, 1, .65f, .75f) : new Color(1, .4f, .35f, .75f);
        foreach (var layer in drop.Layers)
        foreach (var part in Parts(layer, side))
        {
            var dest = Destination(part, side); var raster = Raster(layer, part, side);
            if (layer.Mounted is { } mounted) DrawMounted(canvas, bounds, dest, raster.Texture, raster.Source, mounted, color);
            else canvas.DrawTextureRectRegion(raster.Texture, new(bounds.Position + dest.Position * bounds.Size, dest.Size * bounds.Size), raster.Source, color);
        }
        if (drop.Geometry is { } geometry)
            foreach (var cell in geometry.Cells)
            {
                var polygon = BodyPolygon(cell, side).Select(p => bounds.Position + p * bounds.Size).ToArray();
                canvas.DrawPolyline(polygon.Append(polygon[0]).ToArray(), color, 1);
            }
        canvas.DrawArc(bounds.Position + drop.Anchor * bounds.Size, 5, 0, Mathf.Tau, 20, color, 2);
    }
}
