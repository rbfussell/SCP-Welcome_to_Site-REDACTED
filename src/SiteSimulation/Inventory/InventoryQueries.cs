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
        => InventoryAccess(parentId, regionId).Allowed ? Inventory.Children(parentId, regionId).Where(i => InventoryWear.Visible(Inventory, i)).ToArray() : [];
    /// <summary>Own known equipment only. Revealing a layer is inspection, never access to its storage.</summary>
    public IReadOnlyList<InventoryItem> InventoryPaperdollContents(string regionId, bool showCovered = false, int? layer = null)
    {
        var root = Inventory.Content?.Roots.SingleOrDefault(r => r.ActorId == Player.Id);
        var mount = root?.Regions.SingleOrDefault(r => r.Id == regionId)?.MountSlot;
        if (root is null || mount is null || layer is < 0 or > 9 || layer is not null && mount == "hand") return [];
        return Inventory.Children(root.Id, regionId).Where(i => (layer is null || Inventory.Definition(i.Id).Wear?.Layer == layer)
            && (showCovered || InventoryWear.Visible(Inventory, i))).ToArray();
    }
    public InventoryAccess InventoryMoveAccess(string id)
    {
        if (!Inventory.Items.TryGetValue(id, out var item)) return new(false, "Unknown item");
        var access = InventoryAccess(item.Placement.ParentId, item.Placement.RegionId);
        if (!access.Allowed) return access;
        try { InventoryWear.ValidateAction(Inventory, item, item.Placement); return access; }
        catch (ArgumentException ex) { return new(false, ex.Message); }
    }
    public InventoryAccess InventoryInspectAccess(string id)
    {
        if (!Inventory.Items.TryGetValue(id, out var item)) return new(false, "Unknown item");
        var access = InventoryAccess(item.Placement.ParentId, item.Placement.RegionId);
        return !access.Allowed ? access : InventoryWear.Visible(Inventory, item) ? access : new(false, "Equipment is covered; expose it first");
    }
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
