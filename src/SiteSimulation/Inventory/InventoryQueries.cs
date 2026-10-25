namespace SiteSimulation;

/// <summary>Player-bounded inventory projections and command facade. Diagnostics remain explicitly separate.</summary>
public sealed partial class SpatialSimulation
{
    public InventoryState Inventory => _world.Inventory;
    public InventoryAccess InventoryAccess(string parentId, string regionId)
    {
        try { return InventoryAccessService.Placement(_world, Player.Id, new(parentId, regionId)); }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or NullReferenceException)
        { return new(false, "Unknown compartment"); }
    }
    public IReadOnlyList<InventoryRoot> InventoryRoots => Inventory.Content?.Roots
        .Where(r => InventoryAccessService.Root(_world, Player.Id, r).Allowed).ToArray() ?? [];
    public IReadOnlyList<InventoryItem> InventoryContents(string parentId, string regionId)
        => InventoryAccess(parentId, regionId).Allowed ? Inventory.Children(parentId, regionId) : [];
    public bool HasAccessibleEquipment(string actorId, string capability) => InventoryAccessService.Capability(_world, actorId, capability);
    public InventoryActionQuote QuoteInventory(InventoryRequest request)
    {
        var allowed = InventoryActions.Validate(_world, Player.Id, request, out var reason, out var duration, out var ap);
        return new(allowed, reason, duration, ap);
    }
    public string InventoryCost(InventoryRequest request) => InventoryActions.Validate(_world, Player.Id, request, out var reason, out var duration, out var ap)
        ? $"{duration / 1000.0:0.00}s · {ap} AP in combat" : reason;
    public bool HandleInventory(InventoryRequest request)
    {
        var result = InventoryActions.Start(_world, request, out var reason); LastMessage = reason; return result;
    }
    public bool CancelInventoryHandling()
    {
        if (ManualPause || LocalHold || Combat.Active) { LastMessage = "Cannot cancel paid handling during a hold or tactical action."; return false; }
        InventoryActions.Cancel(_world, "Handling cancelled"); return true;
    }
}
