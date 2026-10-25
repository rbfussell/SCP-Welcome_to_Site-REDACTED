namespace SiteSimulation;

internal static class InventoryContentValidation
{
    public static void Validate(SimulationWorld world)
    {
        var state = world.Inventory;
        if (state.Content is not { } c) return;
        static bool Id(string? id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 100;
        static bool RegionValid(StorageRegion r) => r is not null && Id(r.Id) && Id(r.Name) && r.Columns is > 0 and <= 32
            && r.Rows is > 0 and <= 32 && r.CellMm is > 0 and <= 1000 && r.HeightMm is > 0 and <= 1000 && r.MaxMassGrams is > 0 and <= 100000
            && r.AccessMs is >= 0 and <= 5000 && r.AccessMs % SpatialSimulation.TickMs == 0 && r.AccessAp is >= 0 and <= 5
            && (r.Mask is null || r.Mask.Distinct().Count() == r.Mask.Length && r.Mask.All(i => i >= 0 && i < r.Columns * r.Rows))
            && (r.MountSlot is null || r.Columns == 1 && r.Rows == 1);
        if (c.Definitions is null || c.Roots is null || c.Items is null || c.Regions is null
            || c.Definitions.Any(d => d is null || !Id(d.Id) || !Id(d.Name) || d.Tags is null
                || d.WidthMm is <= 0 or > 1000 || d.LengthMm is <= 0 or > 1000 || d.ThicknessMm is <= 0 or > 1000
                || d.MassGrams is < 0 or > 100000 || d.HandleMs is < 50 or > 5000 || d.HandleMs % SpatialSimulation.TickMs != 0
                || d.HandleAp is < 1 or > 5 || d.Regions is { } regions && (regions.Any(r => !RegionValid(r)) || regions.Select(r => r.Id).Distinct().Count() != regions.Length))
            || c.Definitions.Select(d => d.Id).Distinct().Count() != c.Definitions.Length
            || c.Roots.Any(r => r is null || !Id(r.Id) || !Id(r.Name) || r.Regions is null || r.Regions.Any(region => !RegionValid(region))
                || r.Regions.Select(region => region.Id).Distinct().Count() != r.Regions.Length || r.MaxMassGrams is <= 0 or > 100000
                || (r.ActorId is null) == (r.PropId is null) || r.ActorId is not null && !world.Actors.Any(a => a.Id == r.ActorId)
                || r.PropId is not null && !world.Map.Definition.Props.Any(p => p.Id == r.PropId))
            || c.Roots.Select(r => r.Id).Distinct().Count() != c.Roots.Length
            || c.Items.Any(i => i is null || !Id(i.Id) || !c.Definitions.Any(d => d.Id == i.DefinitionId)
                || c.Roots.Any(r => r.Id == i.Id)) || c.Items.Select(i => i.Id).Distinct().Count() != c.Items.Length)
            throw new ArgumentException("Invalid inventory content.");
        InventorySaveCodec.ValidateGraph(state);
    }
}
