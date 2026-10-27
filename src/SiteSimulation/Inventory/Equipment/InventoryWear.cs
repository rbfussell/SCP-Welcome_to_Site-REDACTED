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
        var coverage = Mount(state, item, item.Placement)!.Coverage;
        return item.GarmentOpen ? coverage.Except(state.Definition(item.Id).Wear!.OpenExposes ?? []) : coverage;
    }

    private static InventoryItem? Blocker(InventoryState state, InventoryItem item, ItemPlacement placement, IEnumerable<string> regions,
        Obstruction purpose)
    {
        var wear = state.Definition(item.Id).Wear!;
        return Worn(state, placement.ParentId).Where(other => other.Id != item.Id && state.Definition(other.Id).Wear!.Layer > wear.Layer)
            .Where(other => purpose switch
            {
                Obstruction.Access => state.Definition(other.Id).Wear!.CoversAccess,
                Obstruction.Removal => state.Definition(other.Id).Wear!.BlocksRemoval,
                _ => true
            })
            .OrderByDescending(other => state.Definition(other.Id).Wear!.Layer).ThenBy(other => other.Id, StringComparer.Ordinal)
            .FirstOrDefault(other => (purpose == Obstruction.Removal ? state.Definition(other.Id).Wear!.RemovalRegions ?? Mount(state, other, other.Placement)!.Coverage : Covered(state, other)).Intersect(regions).Any());
    }

    /// <summary>Partial exposure is enough to draw a garment; manipulating it can still require removing outer layers.</summary>
    public static bool Visible(InventoryState state, InventoryItem item)
    {
        if (Mount(state, item, item.Placement) is not { } mount) return true;
        return mount.Coverage.Any(region => Blocker(state, item, item.Placement, [region], Obstruction.Visual) is null);
    }

    internal static InventoryAccess Compartment(InventoryState state, string itemId, StorageRegion region)
    {
        if (!state.Items.TryGetValue(itemId, out var item) || Mount(state, item, item.Placement) is not { } mount)
            return new(true, "Accessible");
        if (InventoryCoverage.Evaluate(state, item, region) is { } geometric)
            return new(!geometric.Covered, geometric.Reason);
        var blocker = Blocker(state, item, item.Placement, region.AccessBody ?? mount.Coverage, Obstruction.Access);
        return blocker is null ? new(true, "Accessible") : new(false, region.Name + " is covered by " + state.Definition(blocker.Id).Name + "; expose it first");
    }

    internal static void ValidateAction(InventoryState state, InventoryItem item, ItemPlacement placement,
        IEnumerable<string>? accessRegions = null, bool fastening = false)
    {
        if (Mount(state, item, placement) is not { } mount) return;
        var regions = accessRegions ?? mount.Coverage;
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
                if (!Mount(state, left, left.Placement)!.Coverage.Intersect(Mount(state, right, right.Placement)!.Coverage).Any()) continue;
                var l = state.Definition(left.Id).Wear!; var r = state.Definition(right.Id).Wear!;
                if (l.Layer == r.Layer && !(l.OverlapTags is { Length: > 0 } lt && state.Definition(right.Id).Tags.Any(lt.Contains)
                    && r.OverlapTags is { Length: > 0 } rt && state.Definition(left.Id).Tags.Any(rt.Contains)))
                    throw new ArgumentException("Worn items conflict at the same body layer");
                if (l.Layer != r.Layer && (l.Layer < r.Layer ? l : r).Outermost) throw new ArgumentException("Nothing can cover an outermost pack; remove it first");
            }
        }
    }
}
