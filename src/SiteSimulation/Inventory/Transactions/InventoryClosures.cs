namespace SiteSimulation;

/// <summary>Per-fastening presentation verbs and opt-in dismissal policy; neither performs physical work.</summary>
public static class InventoryClosures
{
    public static InventoryClosureAction Toggle(InventoryState state, string itemId, string? regionId = null)
    {
        var item = state.DiagnosticItems.Single(i => i.Id == itemId);
        if (regionId is not null)
        {
            var region = state.Region(itemId, regionId);
            var open = state.IsOpen(itemId, regionId);
            return new(open ? region.SecureVerb : region.OpenVerb,
                new(open ? InventoryActionKind.Close : InventoryActionKind.Open, itemId, RegionId: regionId));
        }
        var wear = state.Definition(itemId).Wear;
        if (wear?.OpenExposes is not { Length: > 0 }) throw new ArgumentException("This garment has no supported fastening.");
        return new(item.GarmentOpen ? wear.SecureVerb : wear.OpenVerb,
            new(item.GarmentOpen ? InventoryActionKind.CloseGarment : InventoryActionKind.OpenGarment, itemId));
    }

    /// <summary>UI dismissal requests only. Explicit securing ignores this convenience preference.</summary>
    public static bool SecureOnClose(InventoryState state, string itemId, string? regionId = null)
    {
        if (regionId is not null)
            return state.Regions.Single(r => r.ItemId == itemId && r.RegionId == regionId).SecureOnClose ?? state.Region(itemId, regionId).SecureOnClose;
        return state.Items[itemId].SecureOnClose ?? state.Definition(itemId).Wear!.SecureOnClose;
    }
    public static InventoryRequest[] OnDismissal(InventoryState state, IEnumerable<InventoryRequest> candidates)
    {
        return candidates.Where(r => r.Kind switch
        {
            InventoryActionKind.Close when r.RegionId is not null && r.Destination is null => SecureOnClose(state, r.ItemId, r.RegionId),
            InventoryActionKind.CloseGarment when r.RegionId is null && r.Destination is null
                => state.Definition(r.ItemId).Wear?.OpenExposes is { Length: > 0 } && SecureOnClose(state, r.ItemId),
            _ => throw new ArgumentException("Dismissal can only propose closing a compartment or garment fastening.")
        }).Distinct().ToArray();
    }
}
