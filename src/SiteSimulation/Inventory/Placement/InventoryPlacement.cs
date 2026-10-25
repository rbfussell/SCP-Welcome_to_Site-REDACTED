namespace SiteSimulation;

/// <summary>Millimetre envelopes and discrete packing footprints. View scale cannot create capacity.</summary>
public static class InventoryPlacement
{
    public static (int Width, int Height) Footprint(ItemDefinition item, StorageRegion region, bool rotated)
        => region.MountSlot is not null ? (1, 1) : ((int)Math.Ceiling((rotated ? item.LengthMm : item.WidthMm) / (double)region.CellMm),
            (int)Math.Ceiling((rotated ? item.WidthMm : item.LengthMm) / (double)region.CellMm));
    internal static void Validate(InventoryState state)
    {
        if (!state.Enabled) return;
        foreach (var item in state.Items.Values)
        {
            _ = InventoryHierarchy.Root(state, item.Id); // Cycle/depth validation precedes recursive mass.
            var definition = state.Definition(item.Id);
            var p = item.Placement;
            var region = state.Region(p.ParentId, p.RegionId);
            if (region.MountSlot is { } mount && mount != "hand" && mount != definition.WearSlot)
                throw new ArgumentException(region.Name + " accepts only matching wearable equipment");
            if (region.AcceptTags is { Length: > 0 } tags && !definition.Tags.Any(tags.Contains))
                throw new ArgumentException(region.Name + " rejects this item category");
            var (width, height) = Footprint(definition, region, p.Rotated);
            if (p.X < 0 || p.Y < 0 || p.X + width > region.Columns || p.Y + height > region.Rows
                || region.MountSlot is null && definition.ThicknessMm > region.HeightMm)
                throw new ArgumentException(definition.Name + " does not physically fit in " + region.Name);
            var cells = Enumerable.Range(p.Y, height).SelectMany(y => Enumerable.Range(p.X, width).Select(x => y * region.Columns + x)).ToArray();
            if (region.Mask is { } mask && cells.Any(c => !mask.Contains(c))) throw new ArgumentException("Item overlaps the compartment's unusable area");
            foreach (var other in state.Items.Values.Where(i => i.Id != item.Id && i.Placement.ParentId == p.ParentId && i.Placement.RegionId == p.RegionId))
            {
                var (w, h) = Footprint(state.Definition(other.Id), region, other.Placement.Rotated);
                if (p.X < other.Placement.X + w && p.X + width > other.Placement.X && p.Y < other.Placement.Y + h && p.Y + height > other.Placement.Y)
                    throw new ArgumentException("Packing footprint overlaps another item");
            }
            if (state.Children(p.ParentId, p.RegionId).Sum(i => InventoryHierarchy.Mass(state, i.Id)) > region.MaxMassGrams)
                throw new ArgumentException(region.Name + " exceeds its load limit");
        }
        foreach (var root in state.Content!.Roots)
            if (InventoryHierarchy.CarriedMass(state, root.Id) > root.MaxMassGrams) throw new ArgumentException(root.Name + " exceeds its total load limit");
    }
}
