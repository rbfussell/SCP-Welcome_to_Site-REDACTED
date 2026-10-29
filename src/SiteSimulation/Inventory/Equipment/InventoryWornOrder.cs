namespace SiteSimulation;

public sealed record WornLayer(string Region, int Layer, int SubLayer = 0);
public sealed record EquipmentEntry(string ItemId, string Group, WornLayer[] Layers, bool Visible, InventoryAccess Access);

/// <summary>Persistent dressing precedence, with local primary depths derived from the current outfit.</summary>
public static class InventoryWornOrder
{
    internal const long MaxOrder = 1_000_000_000;
    public static long Order(InventoryItem item) => item.Placement.WornOrder ?? 0;
    internal static ItemPlacement Destination(InventoryState state, InventoryItem item, ItemPlacement destination)
    {
        if (InventoryWear.Mount(state, item, destination) is null)
        {
            if (destination.WornOrder is not null) throw new ArgumentException("Only worn equipment has a dressing order.");
            return destination;
        }
        var sameRoot = item.Placement.ParentId == destination.ParentId && InventoryWear.Mount(state, item, item.Placement) is not null;
        var order = sameRoot ? Order(item) : InventoryWear.Worn(state, destination.ParentId).Select(Order).DefaultIfEmpty(0).Max() + 1;
        if (order is < 1 or > MaxOrder) throw new ArgumentException("Dressing order exceeds its supported bound.");
        if (destination.WornOrder is { } requested && requested != order)
            throw new ArgumentException("Dressing cannot insert or reorder equipment; remove outer gear first.");
        return destination with { WornOrder = order };
    }
    internal static InventoryRequest Resolve(InventoryState state, InventoryRequest request)
        => request.Kind == InventoryActionKind.Move && request.Destination is { } destination && state.Items.TryGetValue(request.ItemId, out var item)
            ? request with { Destination = Destination(state, item, destination) } : request;

    /// <summary>Physical surface stacks project to region labels; local bands and subpositions are never stored twice.</summary>
    public static WornLayer[] Layers(InventoryState state, string id)
    {
        if (!state.Items.TryGetValue(id, out var item) || InventoryWear.Mount(state, item, item.Placement) is null) return [];
        return Layers(state, item, InventoryWornStack.Build(state, item.Placement.ParentId));
    }
    internal static WornLayer[] Layers(InventoryState state, InventoryItem item, IReadOnlyList<WornCellLayer> stack)
    {
        var root = state.Content!.Roots.Single(r => r.Id == item.Placement.ParentId);
        var body = state.BodyFits!.Body(root.BodyFamily!);
        return stack.Where(c => c.ItemId == item.Id)
            .Select(c => new WornLayer(InventoryWornStack.Region(body, c.Cell), c.Layer, c.SubLayer)).Distinct().ToArray();
    }
    internal static void Validate(InventoryState state)
    {
        foreach (var item in state.Items.Values)
        {
            var worn = InventoryWear.Mount(state, item, item.Placement) is not null;
            if (worn ? item.Placement.WornOrder is not (>= 1 and <= MaxOrder) : item.Placement.WornOrder is not null)
                throw new ArgumentException("Invalid or missing worn dressing order.");
        }
        foreach (var root in state.Content!.Roots.Where(r => r.ActorId is not null))
        {
            var orders = InventoryWear.Worn(state, root.Id).Select(Order).ToArray();
            if (orders.Distinct().Count() != orders.Length) throw new ArgumentException("Duplicate worn dressing order.");
        }
    }
}

public sealed partial class SpatialSimulation
{
    /// <summary>Own known equipment only; rows identify clothing but never reveal its stored contents.</summary>
    public IReadOnlyList<EquipmentEntry> InventoryEquipment
    {
        get
        {
            var root = Inventory.Content?.Roots.SingleOrDefault(r => r.ActorId == Player.Id);
            if (root is null) return [];
            var groups = new (string Name, string[] Regions)[] {
                ("Head / face", ["head", "face"]), ("Chest", ["torso-front", "torso-back", "arms", "hands"]),
                ("Lower body", ["hips-front", "hips-back", "legs"]), ("Feet", ["feet"]) };
            var entries = new List<EquipmentEntry>();
            var stack = InventoryWornStack.Build(Inventory, root.Id);
            foreach (var (name, regions) in groups)
                foreach (var item in InventoryWear.Worn(Inventory, root.Id).OrderByDescending(InventoryWornOrder.Order).ThenBy(i => i.Id, StringComparer.Ordinal))
                {
                    var layers = InventoryWornOrder.Layers(Inventory, item, stack).Where(l => regions.Contains(l.Region)).ToArray();
                    if (layers.Length > 0) entries.Add(new(item.Id, name, layers, InventoryWear.Visible(Inventory, item), InventoryMoveAccess(item.Id)));
                }
            entries.AddRange(Inventory.Children(root.Id, "hand").Select(i => new EquipmentEntry(i.Id, "Held", [], true, InventoryMoveAccess(i.Id))));
            return entries;
        }
    }
    public IReadOnlyList<WornCellLayer> InventoryWornCells => Inventory.Content?.Roots.SingleOrDefault(r => r.ActorId == Player.Id) is { } root
        ? InventoryWornStack.Build(Inventory, root.Id) : [];
}
