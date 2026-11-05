using Godot;
using SiteSimulation;

namespace SitePresentation;

internal sealed record PaperdollAttachmentPlane(float[] Origin, float CellPixels);
internal sealed record PaperdollAttachmentBinding(string Definition, int Column, float[] Anchor, string Surface = "front");
internal sealed record PaperdollMountedArt(Vector2 Origin, Vector2 Anchor, int Rotation, string Surface);

internal sealed partial class InventoryPaperdollArt
{
    /// <summary>Legacy atlas names and new relative item bundles resolve inside the project's Assets tree only.</summary>
    private static bool AssetFile(string file)
    {
        if (string.IsNullOrWhiteSpace(file) || file.Length > 256 || file.Contains(':') || file.Contains('\\') || System.IO.Path.IsPathRooted(file)
            || !file.EndsWith(".png", StringComparison.Ordinal)) return false;
        var root = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://Assets"));
        var resolved = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://Assets/paperdoll-proof/" + file));
        return resolved.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
    private static void ValidateAttachments(PaperdollManifest manifest)
    {
        static bool Point(float[]? p) => p is { Length: 2 } && p.All(n => float.IsFinite(n) && n is >= 0 and <= 1);
        var bindings = manifest.Attachments ?? [];
        if (manifest.Bindings.Any(b => b.AttachmentPlane is { } p && (!Point(p.Origin) || !float.IsFinite(p.CellPixels) || p.CellPixels is <= 0 or > 100))
            || bindings.Length > 128 || bindings.Select(b => (b.Definition, b.Surface)).Distinct().Count() != bindings.Length
            || bindings.Any(b => string.IsNullOrWhiteSpace(b.Definition) || !Point(b.Anchor) || b.Surface is not ("front" or "back")
                || manifest.Frames.SingleOrDefault(f => f.Column == b.Column) is not { Column: > 0, Parts.Length: 1 } frame
                || frame.Parts[0].Destination[0] != 0 || frame.Parts[0].Destination[1] != 0 || frame.Parts[0].BackDestination is not null))
            throw new InvalidOperationException("Invalid bounded paperdoll attachment registration.");
    }
    /// <summary>Compose qualified exterior descendants in the worn root's plane; never render through an unsupported provider.</summary>
    private IEnumerable<PaperdollLayer> AttachedLayers(SpatialSimulation sim, InventoryItem owner, PaperdollBinding binding)
    {
        if (binding.AttachmentPlane is not { } plane) yield break;
        var state = sim.Inventory;
        var slot = state.Region(owner.Placement.ParentId, owner.Placement.RegionId).MountSlot;
        var mount = state.Definition(owner.Id).Wear!.Mounts.Single(m => m.Slot == slot);
        IEnumerable<PaperdollLayer> Children(InventoryItem provider, BodyPose parentPose, string inheritedSurface, int depth)
        {
            if (depth >= InventoryHierarchy.MaxDepth) yield break;
            foreach (var region in (state.Definition(provider.Id).Regions ?? []).Where(r => r.Attachment is not null))
            foreach (var child in state.Children(provider.Id, region.Id).OrderBy(i => i.Id, StringComparer.Ordinal))
            {
                if (child.Placement.AttachmentPose is not { } local) continue;
                var surface = region.Attachment!.Surface == "mounted" ? inheritedSurface : region.Attachment.Surface;
                var art = (_manifest.Attachments ?? []).SingleOrDefault(b => b.Definition == child.DefinitionId && b.Surface == surface);
                if (art is null) continue;
                var pose = InventoryAttachmentFrames.Compose(parentPose, local);
                var pixelOffset = new Vector2(pose.Q + pose.R * .5f, pose.R * .8660254f) * plane.CellPixels;
                var origin = new Vector2(plane.Origin[0], plane.Origin[1]) + pixelOffset / new Vector2(_manifest.CanvasSize[0], _manifest.CanvasSize[1]);
                yield return new(child.Id, art.Column, new(origin, new(art.Anchor[0], art.Anchor[1]), pose.Rotation, surface));
                foreach (var nested in Children(child, pose, surface, depth + 1)) yield return nested;
            }
        }
        foreach (var layer in Children(owner, new(0, 0), mount.BodySurface ?? "front", 0)) yield return layer;
    }
    /// <summary>Own worn items and exterior descendants for honest omission reporting; never scan pocket contents as outfit.</summary>
    private static IEnumerable<InventoryItem> OwnExterior(SpatialSimulation sim)
    {
        IEnumerable<InventoryItem> Children(InventoryItem owner, int depth)
        {
            if (depth >= InventoryHierarchy.MaxDepth) yield break;
            foreach (var region in (sim.Inventory.Definition(owner.Id).Regions ?? []).Where(r => r.Attachment is not null))
            foreach (var child in sim.Inventory.Children(owner.Id, region.Id))
            {
                yield return child;
                foreach (var nested in Children(child, depth + 1)) yield return nested;
            }
        }
        foreach (var item in sim.BodyFitItems.Where(i => sim.Inventory.Region(i.Placement.ParentId, i.Placement.RegionId).MountSlot is { } slot
            && InventoryWear.Accepts(sim.Inventory.Definition(i.Id), slot)))
        {
            yield return item;
            foreach (var child in Children(item, 0)) yield return child;
        }
    }
    private Vector2 MountedLocal(PaperdollMountedArt mounted, Rect2 destination, Vector2 uv)
    {
        var canvas = new Vector2(_manifest.CanvasSize[0], _manifest.CanvasSize[1]);
        return ((uv - mounted.Origin) * canvas).Rotated(-mounted.Rotation * Mathf.Pi / 3) / (destination.Size * canvas) + mounted.Anchor;
    }
    private static void DrawMounted(Control target, Rect2 bounds, Rect2 destination, Texture2D texture, Rect2 source, PaperdollMountedArt mounted, Color tint)
    {
        var size = destination.Size * bounds.Size;
        target.DrawSetTransform(bounds.Position + mounted.Origin * bounds.Size, mounted.Rotation * Mathf.Pi / 3);
        target.DrawTextureRectRegion(texture, new(-mounted.Anchor * size, size), source, tint);
        target.DrawSetTransform(Vector2.Zero);
    }
}
