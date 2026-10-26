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
    /// <summary>Exposed lower clothing at the current opening; inspection never changes its physical parent.</summary>
    public IReadOnlyList<InventoryItem> InventoryUnderlayers(string id)
    {
        if (!Inventory.Items.TryGetValue(id, out var item) || !item.GarmentOpen
            || Inventory.Definition(id).Wear is not { OpenExposes.Length: > 0 } wear
            || !Inventory.Content!.Roots.Any(r => r.Id == item.Placement.ParentId && r.ActorId == Player.Id)) return [];
        return Inventory.DiagnosticItems.Where(i => i.Placement.ParentId == item.Placement.ParentId && i.Id != id)
            .Where(i => Inventory.Definition(i.Id).Wear is { } inner && inner.Layer < wear.Layer
                && inner.Mounts.Any(m => m.Slot == Inventory.Region(i.Placement.ParentId, i.Placement.RegionId).MountSlot
                    && m.Coverage.Intersect(wear.OpenExposes).Any()) && InventoryWear.Visible(Inventory, i))
            .OrderByDescending(i => Inventory.Definition(i.Id).Wear!.Layer).ThenBy(i => i.Id, StringComparer.Ordinal).ToArray();
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
        var result = InventoryActions.Start(_world, request, out var reason);
        if (result) Inventory.SecureQueue.Clear(); // A new explicit action replaces unstarted automatic work.
        LastMessage = reason; return result;
    }
    public bool SecureInventory(IEnumerable<InventoryRequest> requests, bool automatic = false)
    {
        if (automatic)
        {
            var eligible = InventoryClosures.OnDismissal(Inventory, requests);
            if (eligible.Length == 0) { LastMessage = "This gear stays in its current fastening state."; return true; }
            requests = eligible;
        }
        var result = InventorySecuring.Prepare(_world, requests, out var reason, automatic); LastMessage = reason; return result;
    }
    /// <summary>Convenience behavior belongs to this instance/fastening; changing it performs no physical work.</summary>
    public bool SetInventorySecureOnClose(string itemId, string? regionId, bool value)
    {
        if (!Inventory.Items.ContainsKey(itemId)) { LastMessage = "Unknown gear."; return false; }
        InventoryClosureAction target;
        try { target = InventoryClosures.Toggle(Inventory, itemId, regionId); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { LastMessage = "Unknown or unsupported fastening."; return false; }
        if (!InventoryActions.Validate(_world, Player.Id, target.Request, out var reason, out _, out _))
        { LastMessage = reason; return false; }
        if (regionId is null) Inventory.Items[itemId] = Inventory.Items[itemId] with { SecureOnClose = value };
        else
        {
            var index = Inventory.Regions.FindIndex(r => r.ItemId == itemId && r.RegionId == regionId);
            Inventory.Regions[index] = Inventory.Regions[index] with { SecureOnClose = value };
        }
        // This preference changes neither collision/access nor a paid action's revision/cost.
        if (!value) Inventory.SecureQueue.RemoveAll(s => s.Automatic && s.Request.ItemId == itemId && s.Request.RegionId == regionId);
        LastMessage = "Auto-secure on dismissal: " + (value ? "on" : "off") + " for this fastening.";
        return true;
    }
    public bool CancelInventoryHandling()
    {
        if (Inventory.SecureQueue.Count > 0)
        {
            Inventory.SecureQueue.Clear();
            if (Inventory.Pending is null || ManualPause || LocalHold || Combat.Active)
            { LastMessage = "Remaining securing intent canceled; any already paid action still resolves."; return true; }
        }
        if (ManualPause || LocalHold || Combat.Active) { LastMessage = "Cannot cancel paid handling during a hold or tactical action."; return false; }
        InventoryActions.Cancel(_world, "Handling cancelled"); return true;
    }
}
