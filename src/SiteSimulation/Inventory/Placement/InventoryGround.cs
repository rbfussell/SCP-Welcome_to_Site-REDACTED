using System.Globalization;

namespace SiteSimulation;

/// <summary>Map-local floor patches and ordered physical piles. Empty patches are resolved without mutating state.</summary>
public static class InventoryGround
{
    public const string Prefix = "ground:";
    public const string RegionId = "floor";
    public const int Columns = 10, Rows = 10, CellMm = 50;
    private const long MaxOrder = 1_000_000_000;
    private static readonly StorageRegion Floor = new(RegionId, "Ground", Columns, Rows, CellMm, 1000, 100000,
        AccessMs: 500, AccessAp: 2, Sealable: false, SecureOnClose: false);
    public static string Id(Hex cell) => FormattableString.Invariant($"{Prefix}{cell.Q}:{cell.R}");
    internal static InventoryRoot? FindRoot(InventoryState state, string id)
    {
        if (!state.Enabled || id is null || !id.StartsWith(Prefix, StringComparison.Ordinal)) return null;
        var parts = id[Prefix.Length..].Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var q)
            || !int.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var r)) return null;
        var cell = new Hex(q, r);
        return id == Id(cell) && state.GroundCells.Contains(cell)
            ? new(id, FormattableString.Invariant($"Ground · {q}, {r}"), [Floor], MaxMassGrams: Floor.MaxMassGrams, GroundCell: cell) : null;
    }
    public static bool Overlap(InventoryState state, InventoryItem first, InventoryItem second)
    {
        var a = first.Placement; var b = second.Placement;
        if (a.ParentId != b.ParentId || a.RegionId != b.RegionId) return false;
        var region = state.Region(a.ParentId, a.RegionId);
        var (aw, ah) = InventoryPlacement.Footprint(state.Definition(first.Id), region, a.Rotated);
        var (bw, bh) = InventoryPlacement.Footprint(state.Definition(second.Id), region, b.Rotated);
        return a.X < b.X + bw && a.X + aw > b.X && a.Y < b.Y + bh && a.Y + ah > b.Y;
    }
    /// <summary>Direct covering items; callers checking nested contents also traverse their ancestor locations.</summary>
    public static IReadOnlyList<InventoryItem> Covering(InventoryState state, InventoryItem item)
        => item.Placement.GroundOrder is not { } order ? [] : state.Children(item.Placement.ParentId, item.Placement.RegionId)
            .Where(other => other.Placement.GroundOrder > order && Overlap(state, item, other))
            .OrderByDescending(other => other.Placement.GroundOrder).ToArray();

    internal static ItemPlacement Destination(InventoryState state, InventoryItem item, ItemPlacement target)
    {
        if (state.FindRoot(target.ParentId)?.GroundCell is null)
        {
            if (target.GroundOrder is not null) throw new ArgumentException("Pile order is only valid on the ground.");
            return target;
        }
        var same = item.Placement with { GroundOrder = null } == target with { GroundOrder = null };
        var order = same ? item.Placement.GroundOrder!.Value
            : state.Children(target.ParentId, target.RegionId).Where(i => i.Id != item.Id).Select(i => i.Placement.GroundOrder ?? 0).DefaultIfEmpty(0).Max() + 1;
        if (order is < 1 or > MaxOrder || target.GroundOrder is { } supplied && supplied != order)
            throw new ArgumentException("Ground placement must go on top of the pile.");
        return target with { GroundOrder = order };
    }
    internal static void Validate(InventoryState state)
    {
        foreach (var item in state.Items.Values)
            if (state.FindRoot(item.Placement.ParentId)?.GroundCell is not null
                ? item.Placement.GroundOrder is not (>= 1 and <= MaxOrder)
                : item.Placement.GroundOrder is not null)
                throw new ArgumentException("Invalid or missing ground pile order.");
        foreach (var pile in state.Items.Values.Where(i => i.Placement.GroundOrder is not null).GroupBy(i => i.Placement.ParentId))
        {
            var items = pile.ToArray(); var region = state.Region(pile.Key, RegionId);
            if (items.Select(i => i.Placement.GroundOrder).Distinct().Count() != items.Length)
                throw new ArgumentException("Duplicate ground pile order.");
            if (items.Sum(i => InventoryHierarchy.Mass(state, i.Id)) > region.MaxMassGrams)
                throw new ArgumentException("Ground patch exceeds its load limit.");
            var height = new int[region.Rows * region.Columns];
            foreach (var item in items)
            {
                var p = item.Placement; var d = state.Definition(item.Id); var (w, h) = InventoryPlacement.Footprint(d, region, p.Rotated);
                for (var y = p.Y; y < p.Y + h; y++)
                for (var x = p.X; x < p.X + w; x++)
                    if ((height[y * region.Columns + x] += d.ThicknessMm) > region.HeightMm)
                        throw new ArgumentException("Ground pile is too high.");
            }
        }
    }
}

public sealed partial class SpatialSimulation
{
    /// <summary>The physical hex beneath the player, not a portable inventory following the actor.</summary>
    public InventoryRoot? InventoryGroundRoot => Inventory.FindRoot(InventoryGround.Id(Hex.FromWorld(Player.Position)));
    public InventoryRequest GroundDropRequest(string id, bool rotated = false)
    {
        var root = InventoryGroundRoot;
        if (root is null || !Inventory.Items.TryGetValue(id, out var item)) return new(InventoryActionKind.Move, id);
        var region = root.Regions[0]; var (w, h) = InventoryPlacement.Footprint(Inventory.Definition(id), region, rotated);
        // The explicit Put down shortcut prefers free floor, then the top of an existing pile.
        for (var y = 0; y <= region.Rows - h; y++)
        for (var x = 0; x <= region.Columns - w; x++)
        {
            var placed = item with { Placement = new(root.Id, region.Id, x, y, rotated) };
            if (!Inventory.Children(root.Id, region.Id).Any(other => other.Id != id && InventoryGround.Overlap(Inventory, placed, other)))
                return new(InventoryActionKind.Move, id, placed.Placement);
        }
        return new(InventoryActionKind.Move, id, new(root.Id, region.Id, Rotated: rotated));
    }
}
