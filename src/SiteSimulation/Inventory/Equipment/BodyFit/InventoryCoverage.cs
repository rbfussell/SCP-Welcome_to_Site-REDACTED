namespace SiteSimulation;

/// <summary>Body-surface overlap for one compartment mouth. Physical lids and removal restraints are separate.</summary>
public static class InventoryCoverage
{
    internal static BodySurfaceCell[] Project(BodySurfaceMask[] masks, BodyPose pose, WearMount mount)
        => masks.SelectMany(mask => mask.Cells.Select(cell => new BodySurfaceCell(
            mask.Surface == "mounted" ? mount.BodySurface! : mask.Surface, BodyFitGeometry.Transform(cell, pose)))).ToArray();

    internal static PocketCoverage? Evaluate(InventoryState state, InventoryItem item, StorageRegion region)
    {
        var mount = InventoryWear.Mount(state, item, item.Placement);
        var shape = InventoryBodyFit.Shape(state, item.Id);
        if (mount is null || shape is null || region.AccessFootprint is null) return null;
        var mouth = Project(region.AccessFootprint, InventoryBodyFit.Pose(state, item)!, mount);
        var blockers = new List<string>(); var blocked = new HashSet<BodySurfaceCell>();
        var fallback = false;
        foreach (var outer in InventoryWear.Worn(state, item.Placement.ParentId)
            .Where(other => other.Id != item.Id && state.Definition(other.Id).Wear is { CoversAccess: true } wear
                && wear.Layer > state.Definition(item.Id).Wear!.Layer)
            .OrderByDescending(other => state.Definition(other.Id).Wear!.Layer).ThenBy(other => other.Id, StringComparer.Ordinal))
        {
            var coverShape = InventoryBodyFit.Shape(state, outer.Id);
            if (coverShape?.Coverage is { } coverage)
            {
                var masks = outer.GarmentOpen ? coverage.Open : coverage.Closed;
                var cover = Project(masks, InventoryBodyFit.Pose(state, outer)!, InventoryWear.Mount(state, outer, outer.Placement)!).ToHashSet();
                var intersection = mouth.Where(cover.Contains).ToArray();
                if (intersection.Length == 0) continue;
                blocked.UnionWith(intersection);
            }
            else
            {
                // Unmapped outer gear retains the conservative, existing region rule.
                if (!InventoryWear.Covered(state, outer).Intersect(region.AccessBody ?? mount.Coverage).Any()) continue;
                fallback = true;
            }
            blockers.Add(outer.Id);
        }
        var reason = blockers.Count == 0 ? "Access footprint is uncovered."
            : region.Name + " is covered by " + string.Join(", ", blockers.Select(id => state.Definition(id).Name)) + "; expose it first";
        return new(item.Id, region.Id, blockers.Count > 0, reason, mouth, blocked.OrderBy(c => c.Surface, StringComparer.Ordinal)
            .ThenBy(c => c.Cell.R).ThenBy(c => c.Cell.Q).ToArray(), blockers.ToArray(), fallback);
    }
}

public sealed partial class SpatialSimulation
{
    /// <summary>Known own worn pockets only. Returns coverage geometry, never container contents.</summary>
    public PocketCoverage? InventoryPocketCoverage(string itemId, string regionId)
    {
        var item = Inventory.DiagnosticItems.SingleOrDefault(i => i.Id == itemId);
        var root = Inventory.Content?.Roots.SingleOrDefault(r => r.ActorId == Player.Id);
        if (item is null || root is null || item.Placement.ParentId != root.Id) return null;
        var region = Inventory.Definition(itemId).Regions?.SingleOrDefault(r => r.Id == regionId);
        return region is null ? null : InventoryCoverage.Evaluate(Inventory, item, region);
    }
}
