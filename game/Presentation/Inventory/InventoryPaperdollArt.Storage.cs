using Godot;
using SiteSimulation;

namespace SitePresentation;

internal sealed record PaperdollStorageTarget(string Owner, string Region, PaperdollLayer Layer);

internal sealed partial class InventoryPaperdollArt
{
    /// <summary>Only topmost registered storage material offers an interior; straps and transparent gaps never do.</summary>
    internal PaperdollStorageTarget? StorageTarget(SpatialSimulation sim, string side, Vector2 size, Vector2 at)
    {
        var bounds = Bounds(size); var uv = (at - bounds.Position) / bounds.Size;
        if (Pick(sim, side, size, at) is not { } id) return null;
        var layer = Layers(sim).Single(l => l.ItemId == id);
        foreach (var part in Parts(layer, side).Reverse())
        {
            if (part.DropRegion is not { } region || !OpaquePart(layer, part, side, uv)) continue;
            if (!(sim.Inventory.Definition(id).Regions ?? []).Any(r => r.Id == region && r.Attachment is null && r.MountSlot is null))
                throw new InvalidOperationException("Paperdoll storage registration must name a real interior: " + id + "/" + region);
            return new(id, region, layer);
        }
        return null;
    }
}
