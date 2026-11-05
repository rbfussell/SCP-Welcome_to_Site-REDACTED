namespace SiteSimulation;

public static class InventoryHierarchy
{
    public const int MaxDepth = 8;
    public static InventoryRoot Root(InventoryState state, string id)
    {
        for (var depth = 0; depth <= MaxDepth; depth++)
        {
            var root = state.FindRoot(id);
            if (root is not null) return root;
            id = state.Items[id].Placement.ParentId;
        }
        throw new ArgumentException("Inventory hierarchy is cyclic or too deep.");
    }
    public static int Mass(InventoryState state, string id) => state.Definition(id).MassGrams
        + state.Items.Values.Where(i => i.Placement.ParentId == id).Sum(i => Mass(state, i.Id));
    public static int CarriedMass(InventoryState state, string rootId) => state.Items.Values.Where(i => i.Placement.ParentId == rootId).Sum(i => Mass(state, i.Id));
    public static string Breadcrumb(InventoryState state, string id)
    {
        var parts = new List<string>();
        while (state.Items.TryGetValue(id, out var item))
        { parts.Add(state.Definition(id).Name); id = item.Placement.ParentId; }
        parts.Add(state.Name(id)); parts.Reverse();
        return string.Join(" / ", parts);
    }
}
