namespace SiteSimulation;

/// <summary>Exposed diagnostic cells and normal removal feedback. No contents or final sprite-alpha data.</summary>
public sealed record BodyEquipmentTarget(string ItemId, BodyHex[] Cells, InventoryAccess MoveAccess);

public sealed partial class SpatialSimulation
{
    /// <summary>Own exposed worn material on one side. Open covers and dressing order resolve occlusion; no packed contents or foreign outfit is queried.</summary>
    public IReadOnlyList<BodyEquipmentTarget> InventoryBodySurface(string surface)
    {
        if (surface is not ("front" or "back") || Inventory.Content?.Roots.SingleOrDefault(r => r.ActorId == Player.Id) is not { } root) return [];
        var occupied = new HashSet<BodyHex>(); var result = new List<BodyEquipmentTarget>();
        foreach (var item in InventoryWear.Worn(Inventory, root.Id).OrderByDescending(InventoryWornOrder.Order))
        {
            var material = InventoryWornStack.Cells(Inventory, item, open: true).Where(c => c.Surface == surface).Select(c => c.Cell).ToArray();
            var exposed = material.Where(c => !occupied.Contains(c)).ToArray();
            occupied.UnionWith(material);
            if (exposed.Length > 0) result.Add(new(item.Id, exposed, InventoryMoveAccess(item.Id)));
        }
        return result;
    }
    /// <summary>Pick one exposed own-worn target using anatomical coordinates, independent of screen mirroring.</summary>
    public BodyEquipmentTarget? InventoryBodyPick(string surface, BodyHex cell)
        => InventoryBodySurface(surface).SingleOrDefault(target => target.Cells.Contains(cell));
}
