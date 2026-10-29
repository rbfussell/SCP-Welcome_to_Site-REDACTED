namespace SiteSimulation;

/// <summary>Physical worn coverage and dressing dependencies, independent of paperdoll camera and storage packing.</summary>
public static class InventoryWear
{
    private enum Obstruction { Visual, Access, Removal }
    public static readonly IReadOnlySet<string> BodyRegions = new HashSet<string>(StringComparer.Ordinal)
        { "head", "face", "torso-front", "torso-back", "arms", "hands", "hips-front", "hips-back", "legs", "feet" };

    public static bool Accepts(ItemDefinition definition, string mount) => definition.Wear is { } wear
        ? wear.Mounts.Any(m => m.Slot == mount) : definition.WearSlot == mount;

    internal static WearMount? Mount(InventoryState state, InventoryItem item, ItemPlacement placement)
        => state.Content!.Roots.Any(r => r.ActorId is not null && r.Id == placement.ParentId)
            ? state.Definition(item.Id).Wear?.Mounts.SingleOrDefault(m => m.Slot == state.Region(placement.ParentId, placement.RegionId).MountSlot) : null;

    internal static IEnumerable<InventoryItem> Worn(InventoryState state, string root)
        => state.Items.Values.Where(i => i.Placement.ParentId == root && Mount(state, i, i.Placement) is not null);

    internal static IEnumerable<string> Covered(InventoryState state, InventoryItem item)
    {
        if (state.Definition(item.Id).Wear!.OverlayLimit is not null)
        {
            var root = state.Content!.Roots.Single(r => r.Id == item.Placement.ParentId);
            return InventoryWornStack.Cells(state, item, true).Select(c => InventoryWornStack.Region(state.BodyFits!.Body(root.BodyFamily!), c)).Distinct();
        }
        var coverage = Mount(state, item, item.Placement)!.Coverage;
        return item.GarmentOpen ? coverage.Except(state.Definition(item.Id).Wear!.OpenExposes ?? []) : coverage;
    }

    private static InventoryItem? Blocker(InventoryState state, InventoryItem item, ItemPlacement placement, IEnumerable<string> regions,
        Obstruction purpose)
    {
        var order = placement.WornOrder ?? InventoryWornOrder.Order(item);
        return Worn(state, placement.ParentId).Where(other => other.Id != item.Id && InventoryWornOrder.Order(other) > order)
            .Where(other => purpose switch
            {
                Obstruction.Access => state.Definition(other.Id).Wear!.CoversAccess,
                Obstruction.Removal => state.Definition(other.Id).Wear!.BlocksRemoval,
                _ => true
            })
            .OrderByDescending(InventoryWornOrder.Order).ThenBy(other => other.Id, StringComparer.Ordinal)
            .FirstOrDefault(other => Interferes(state, other, item, regions, purpose));
    }
    private static bool Interferes(InventoryState state, InventoryItem outer, InventoryItem inner, IEnumerable<string> regions, Obstruction purpose)
    {
        var wear = state.Definition(inner.Id).Wear!;
        if (wear.OverlayLimit is not null) return InventoryWornStack.OverlayIntersects(state, outer, inner, purpose != Obstruction.Removal);
        if (wear.Outermost && !InventoryWornStack.OverlayIntersects(state, outer, inner, purpose != Obstruction.Removal)) return false;
        var blockedRegions = purpose == Obstruction.Removal
            ? state.Definition(outer.Id).Wear!.RemovalRegions ?? Mount(state, outer, outer.Placement)!.Coverage : Covered(state, outer);
        return blockedRegions.Intersect(regions).Any();
    }

    /// <summary>Partial exposure is enough to draw a garment; manipulating it can still require removing outer layers.</summary>
    public static bool Visible(InventoryState state, InventoryItem item)
    {
        if (Mount(state, item, item.Placement) is not { } mount) return true;
        var cells = InventoryWornStack.Cells(state, item);
        if (cells.Length > 0)
        {
            var occluded = Worn(state, item.Placement.ParentId).Where(i => InventoryWornOrder.Order(i) > InventoryWornOrder.Order(item))
                .SelectMany(i => InventoryWornStack.Cells(state, i, true)).ToHashSet();
            return cells.Any(c => !occluded.Contains(c));
        }
        return mount.Coverage.Any(region => Blocker(state, item, item.Placement, [region], Obstruction.Visual) is null);
    }

    internal static InventoryAccess Compartment(InventoryState state, string itemId, StorageRegion region)
    {
        if (!state.Items.TryGetValue(itemId, out var item) || Mount(state, item, item.Placement) is not { } mount)
            return new(true, "Accessible");
        if (region.Attachment is not null) return new(true, "Exterior contacts validate individually.");
        if (InventoryCoverage.Evaluate(state, item, region) is { } geometric)
            return new(!geometric.Covered, geometric.Reason);
        var blocker = Blocker(state, item, item.Placement, region.AccessBody ?? mount.Coverage, Obstruction.Access);
        return blocker is null ? new(true, "Accessible") : new(false, region.Name + " is covered by " + state.Definition(blocker.Id).Name + "; expose it first");
    }

    internal static void ValidateAction(InventoryState state, InventoryItem item, ItemPlacement placement,
        IEnumerable<string>? accessRegions = null, bool fastening = false)
    {
        item = item with { Placement = placement };
        if (Mount(state, item, placement) is not { } mount) return;
        var regions = accessRegions ?? (state.Definition(item.Id).Wear!.OverlayLimit is not null ? Covered(state, item) : mount.Coverage);
        if (fastening && state.Definition(item.Id).Wear!.FasteningFootprint is { } masks)
        {
            var zipper = InventoryCoverage.Project(masks, InventoryBodyFit.Pose(state, item)!, mount).ToHashSet();
            var strap = Worn(state, placement.ParentId).Where(i => InventoryWornOrder.Order(i) > InventoryWornOrder.Order(item)
                && state.Definition(i.Id).Wear!.BlocksFastenings).FirstOrDefault(i => InventoryWornStack.Cells(state, i).Any(zipper.Contains));
            if (strap is not null) throw new ArgumentException("Remove " + state.Definition(strap.Id).Name + " before operating this fastening.");
        }
        if (Blocker(state, item, placement, regions, Obstruction.Access) is { } cover)
            throw new ArgumentException("Open or remove " + state.Definition(cover.Id).Name + " before accessing " + state.Definition(item.Id).Name);
        if (!fastening && Blocker(state, item, placement, regions, Obstruction.Removal) is { } blocker)
            throw new ArgumentException("Remove " + state.Definition(blocker.Id).Name + " before changing " + state.Definition(item.Id).Name);
    }

    internal static void ValidateGraph(InventoryState state)
    {
        foreach (var root in state.Content!.Roots.Where(r => r.ActorId is not null))
        {
            var worn = Worn(state, root.Id).OrderBy(i => i.Id, StringComparer.Ordinal).ToArray();
            for (var a = 0; a < worn.Length; a++)
            for (var b = a + 1; b < worn.Length; b++)
            {
                var left = worn[a]; var right = worn[b];
                if (!InventoryWornStack.Overlaps(state, left, right)) continue;
                var inner = InventoryWornOrder.Order(left) < InventoryWornOrder.Order(right) ? left : right;
                if (state.Definition(inner.Id).Wear!.Outermost)
                    throw new ArgumentException("Nothing can cover outermost " + state.Definition(inner.Id).Name + "; remove it first");
            }
            _ = InventoryWornStack.Build(state, root.Id);
            InventoryWearFit.ValidateWorn(state, worn);
        }
    }
}
