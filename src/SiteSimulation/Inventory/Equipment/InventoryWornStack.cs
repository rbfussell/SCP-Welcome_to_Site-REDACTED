namespace SiteSimulation;

public sealed record WornCellLayer(string ItemId, BodySurfaceCell Cell, int Layer, int SubLayer, int BandCount);

/// <summary>One physical projection for bands, strap limits and attachment exposure. No sprite pixels determine fit.</summary>
public static class InventoryWornStack
{
    public static BodySurfaceCell[] Cells(InventoryState state, InventoryItem item, bool open = false)
    {
        var mount = InventoryWear.Mount(state, item, item.Placement);
        var shape = InventoryBodyFit.Shape(state, item.Id);
        if (mount is null || shape is null) return [];
        if (shape.LinearSpan is not null)
        {
            var pose = InventoryBodyFit.Pose(state, item)!;
            var fitted = BodyFitGeometry.Evaluate(state.BodyFits!.Body(shape.BodyFamily), shape, pose).Cells;
            var masks = shape.Coverage is { } strapCoverage ? open && item.GarmentOpen ? strapCoverage.Open : strapCoverage.Closed : null;
            var sides = masks?.Select(m => m.Surface == "mounted" ? mount.BodySurface! : m.Surface).ToArray()
                ?? (mount.BodySurface is { } side ? [side] : new[] { "front", "back" });
            return fitted.SelectMany(c => sides.Select(s => new BodySurfaceCell(s, c))).ToArray();
        }
        if (shape.Coverage is { } coverage)
            return InventoryCoverage.Project(open && item.GarmentOpen ? coverage.Open : coverage.Closed, InventoryBodyFit.Pose(state, item)!, mount);
        // Legacy unmapped artwork still has a physical fitted shape. A mounted pack uses only its specified surface.
        var surfaces = mount.BodySurface is { } surface ? new[] { surface } : new[] { "front", "back" };
        return BodyFitGeometry.Cells(shape.Rows).Select(c => new BodyHex(c.Q - shape.DefaultPose.Q, c.R - shape.DefaultPose.R))
            .SelectMany(c => surfaces.Select(s => new BodySurfaceCell(s, BodyFitGeometry.Transform(c, InventoryBodyFit.Pose(state, item)!)))).ToArray();
    }
    public static IReadOnlyList<WornCellLayer> Build(InventoryState state, string root)
    {
        var worn = InventoryWear.Worn(state, root).OrderBy(InventoryWornOrder.Order).ToArray();
        var positions = worn.SelectMany(i => Cells(state, i).Select(c => (Item: i, Cell: c))).GroupBy(p => p.Cell);
        var result = new List<WornCellLayer>();
        foreach (var position in positions.OrderBy(g => g.Key.Surface, StringComparer.Ordinal).ThenBy(g => g.Key.Cell.R).ThenBy(g => g.Key.Cell.Q))
        {
            var layer = -1; var count = 0; var limits = new List<int>();
            foreach (var (item, cell) in position)
            {
                if (state.Definition(item.Id).Wear!.OverlayLimit is { } limit)
                {
                    if (layer < 0) layer = 0;
                    count++; limits.Add(limit);
                    if (limits.Any(max => count > max))
                        throw new ArgumentException($"Overlay limit at {cell.Surface} {cell.Cell.Q},{cell.Cell.R}: {count} items in this band; move a strap or remove it first.");
                }
                else { layer++; count = 1; limits.Clear(); }
                result.Add(new(item.Id, cell, layer, count - 1, count));
            }
        }
        return result;
    }
    internal static string Region(BodyMask body, BodySurfaceCell cell) => body.Zones?.SingleOrDefault(z => z.Surface == cell.Surface
        && cell.Cell.R >= z.FirstRow && cell.Cell.R <= z.LastRow)?.Region ?? throw new ArgumentException("Body surface cell has no anatomical zone.");
    internal static bool Overlaps(InventoryState state, InventoryItem first, InventoryItem second)
        => Cells(state, first).Intersect(Cells(state, second)).Any();
    internal static bool OverlayIntersects(InventoryState state, InventoryItem outer, InventoryItem inner, bool open = false)
        => Cells(state, outer, open).Intersect(Cells(state, inner)).Any();
}
