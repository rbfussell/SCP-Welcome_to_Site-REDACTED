namespace SiteSimulation;

/// <summary>Pure rectangular packing in one region. Capacity failures are distinguished from search limits.</summary>
internal static class InventoryPacking
{
    internal const int SearchBudget = 60000;
    private sealed record Shape(string Id, int Width, int Height, bool PreferredRotation);
    private sealed record Spot(int X, int Y, bool Rotated, int Width, int Height, int Score);

    public static InventoryPackingPlan Plan(InventoryState state, string owner, string regionId, string? incoming)
    {
        var region = state.Region(owner, regionId);
        if (state.FindRoot(owner)?.GroundCell is not null) throw new ArgumentException("Place floor items directly; ground piles are not auto-arranged.");
        if (region.MountSlot is not null || region.Attachment is not null) throw new ArgumentException("Arrange applies to storage, not body or attachment mounts.");
        var ids = state.Children(owner, regionId).Select(i => i.Id).Where(id => id != incoming).ToList();
        if (incoming is not null) ids.Add(incoming);
        var shapes = ids.Select(id =>
        {
            var definition = state.Definition(id);
            if (definition.ThicknessMm > region.HeightMm) throw new ArgumentException("Item is too thick for this compartment.");
            if (region.AcceptTags is { Length: > 0 } tags && !definition.Tags.Any(tags.Contains)) throw new ArgumentException("Compartment rejects this item category.");
            var size = InventoryPlacement.Footprint(definition, region, false);
            return new Shape(id, size.Width, size.Height, state.Items[id].Placement.Rotated);
        }).ToArray();
        // Moving a child out of a box already in this region must not count it twice.
        var massState = state;
        if (incoming is not null)
        {
            massState = new InventoryState(state);
            massState.Items[incoming] = massState.Items[incoming] with { Placement = new(owner, regionId) };
        }
        if (ids.Sum(id => InventoryHierarchy.Mass(massState, id)) > region.MaxMassGrams) throw new ArgumentException("Compartment load limit exceeded.");
        var available = region.Mask?.Length ?? region.Columns * region.Rows;
        if (shapes.Sum(s => s.Width * s.Height) > available) throw new ArgumentException("Container full: insufficient usable cells; rearranging cannot help.");
        var cells = new bool[region.Columns * region.Rows];
        for (var i = 0; i < cells.Length; i++) cells[i] = region.Mask is not null && !region.Mask.Contains(i);
        bool Fits(bool[] occupied, int x, int y, int width, int height)
        {
            if (x + width > region.Columns || y + height > region.Rows) return false;
            for (var row = y; row < y + height; row++)
            for (var col = x; col < x + width; col++) if (occupied[row * region.Columns + col]) return false;
            return true;
        }
        void Mark(Spot spot, bool occupied)
        {
            for (var y = spot.Y; y < spot.Y + spot.Height; y++)
            for (var x = spot.X; x < spot.X + spot.Width; x++) cells[y * region.Columns + x] = occupied;
        }
        var examined = 0;
        IEnumerable<Spot> Positions(Shape shape)
        {
            var choices = new List<Spot>();
            foreach (var rotated in new[] { shape.PreferredRotation, !shape.PreferredRotation }.Distinct())
            {
                if (shape.Width == shape.Height && rotated != shape.PreferredRotation) continue;
                var w = rotated ? shape.Height : shape.Width; var h = rotated ? shape.Width : shape.Height;
                for (var y = 0; y <= region.Rows - h; y++)
                for (var x = 0; x <= region.Columns - w; x++)
                {
                    if (++examined > 200000) throw new ArgumentException("Packing search limit reached; current contents are unchanged. Try arranging a smaller selection.");
                    if (Fits(cells, x, y, w, h))
                    {
                        // Compact into the upper-left, preferring contact instead of isolated holes.
                        var contact = (x == 0 ? h : 0) + (y == 0 ? w : 0);
                        for (var row = y; row < y + h; row++) if (x > 0 && cells[row * region.Columns + x - 1]) contact++;
                        for (var col = x; col < x + w; col++) if (y > 0 && cells[(y - 1) * region.Columns + col]) contact++;
                        choices.Add(new(x, y, rotated, w, h, (y + h) * 100000 + (x + w) * 1000 + y * 32 + x - contact * 5));
                    }
                }
            }
            return choices.OrderBy(p => p.Score).ThenBy(p => p.Rotated == shape.PreferredRotation ? 0 : 1);
        }
        var placements = new Dictionary<string, ItemPlacement>(StringComparer.Ordinal);
        if (incoming is not null)
        {
            foreach (var id in ids.Where(id => id != incoming))
            {
                var item = state.Items[id]; var size = InventoryPlacement.Footprint(state.Definition(id), region, item.Placement.Rotated);
                Mark(new(item.Placement.X, item.Placement.Y, item.Placement.Rotated, size.Width, size.Height, 0), true);
            }
            var direct = Positions(shapes.Single(s => s.Id == incoming)).FirstOrDefault();
            if (direct is not null) placements[incoming] = new(owner, regionId, direct.X, direct.Y, direct.Rotated);
            if (placements.Count != 0) return Build(false);
            for (var i = 0; i < cells.Length; i++) cells[i] = region.Mask is not null && !region.Mask.Contains(i);
        }
        var ordered = shapes.OrderByDescending(s => s.Width * s.Height).ThenByDescending(s => Math.Max(s.Width, s.Height))
            .ThenBy(s => state.Definition(s.Id).Id, StringComparer.Ordinal).ThenBy(s => s.Id, StringComparer.Ordinal).ToArray();
        var nodes = 0; var exhausted = false;
        bool Search(int index)
        {
            if (index == ordered.Length) return true;
            if (++nodes > SearchBudget) { exhausted = true; return false; }
            var shape = ordered[index];
            foreach (var spot in Positions(shape))
            {
                Mark(spot, true); placements[shape.Id] = new(owner, regionId, spot.X, spot.Y, spot.Rotated);
                if (Search(index + 1)) return true;
                placements.Remove(shape.Id); Mark(spot, false);
                if (exhausted) return false;
            }
            return false;
        }
        if (!Search(0)) throw new ArgumentException(exhausted
            ? "Packing search limit reached; current contents are unchanged. Try arranging a smaller selection."
            : "Container full: its shapes cannot fit, even after arranging.");
        return Build(true);

        InventoryPackingPlan Build(bool repacked)
        {
            var moves = placements.Where(p => p.Value != state.Items[p.Key].Placement).OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => new InventoryPackingMove(p.Key, state.Items[p.Key].Placement, p.Value)).ToArray();
            var candidate = new InventoryState(state);
            foreach (var move in moves) candidate.Items[move.ItemId] = candidate.Items[move.ItemId] with { Placement = move.To };
            InventoryPlacement.Validate(candidate); // Also checks root load, hierarchy/cycles and all other regions.
            if (moves.Length == 0) throw new ArgumentException("Already arranged; no handling needed.");
            return new(moves, repacked);
        }
    }
}
