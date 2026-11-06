namespace SiteSimulation;

internal static class InventoryContentValidation
{
    public static void Validate(SimulationWorld world)
    {
        var state = world.Inventory;
        if (state.Content is not { } c) return;
        static bool Id(string? id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 100;
        static bool Body(string[]? regions) => regions is { Length: > 0 and <= 10 } && regions.Distinct().Count() == regions.Length
            && regions.All(InventoryWear.BodyRegions.Contains);
        static bool WearValid(WearProfile w) => w.Layer is >= 0 and <= 9 && InventoryWearFit.ProfileValid(w.Fit) && w.Mounts is { Length: > 0 and <= 10 }
            && (w.OverlayLimit is null or (>= 1 and <= 16))
            && w.Mounts.All(m => m is not null && Id(m.Slot) && Body(m.Coverage) && (m.BodySurface is null or "front" or "back")) && w.Mounts.Select(m => m.Slot).Distinct().Count() == w.Mounts.Length
            && (w.RemovalRegions is null || Body(w.RemovalRegions))
            && (w.OpenExposes is null || Body(w.OpenExposes) && w.OpenExposes.All(r => w.Mounts.Any(m => m.Coverage.Contains(r))))
            && (w.OverlapTags is null || w.OverlapTags.Length <= 32 && w.OverlapTags.All(Id) && w.OverlapTags.Distinct().Count() == w.OverlapTags.Length)
            && Id(w.SecureVerb) && w.SecureVerb.Length <= 32 && Id(w.OpenVerb) && w.OpenVerb.Length <= 32
            && w.FastenMs is >= 50 and <= 5000 && w.FastenMs % SpatialSimulation.TickMs == 0 && w.FastenAp is >= 1 and <= 5;
        static bool RegionValid(StorageRegion r) => r is not null && Id(r.Id) && Id(r.Name) && r.Columns is > 0 and <= 32
            && r.Rows is > 0 and <= 32 && r.CellMm is > 0 and <= 1000 && r.HeightMm is > 0 and <= 1000 && r.MaxMassGrams is > 0 and <= 100000
            && Id(r.SecureVerb) && r.SecureVerb.Length <= 32 && Id(r.OpenVerb) && r.OpenVerb.Length <= 32
            && r.AccessMs is >= 0 and <= 5000 && r.AccessMs % SpatialSimulation.TickMs == 0 && r.AccessAp is >= 0 and <= 5
            && (r.Mask is null || r.Mask.Distinct().Count() == r.Mask.Length && r.Mask.All(i => i >= 0 && i < r.Columns * r.Rows))
            && (r.MountSlot is null || r.Columns == 1 && r.Rows == 1) && (r.AccessBody is null || Body(r.AccessBody))
            && InventoryAttachments.SurfaceValid(r) && (r.Sealable || !r.SecureOnClose);
        if (c.Definitions is null || c.Roots is null || c.Items is null || c.Regions is null
            || c.Definitions.Any(d => d is null || !Id(d.Id) || !Id(d.Name) || d.Tags is null
                || d.WidthMm is <= 0 or > 1000 || d.LengthMm is <= 0 or > 1000 || d.ThicknessMm is <= 0 or > 1000
                || d.MassGrams is < 0 or > 100000 || d.HandleMs is < 50 or > 5000 || d.HandleMs % SpatialSimulation.TickMs != 0
                || d.HandleAp is < 1 or > 5 || d.Wear is not null && !WearValid(d.Wear)
                || d.Wear?.FitProfile is { } fit && !state.BodyFits!.Content.Shapes.Any(s => s.Id == fit)
                || d.Credential is { } credential && (!Id(credential.Issuer) || credential.Clearance is < 0 or > 5
                    || credential.SubjectId is { } subject && !world.Actors.Any(a => a.Id == subject))
                || !InventoryAttachments.ProfileValid(d.Attachment)
                || d.DefaultDropRegion is not null && d.Regions?.Any(r => r is not null && r.Id == d.DefaultDropRegion && r.MountSlot is null && r.Attachment is null) != true
                || d.Regions is { } regions && (regions.Any(r => !RegionValid(r)) || regions.Select(r => r.Id).Distinct().Count() != regions.Length))
            || c.Definitions.Select(d => d.Id).Distinct().Count() != c.Definitions.Length
            || c.Roots.Any(r => r is null || !Id(r.Id) || !Id(r.Name) || r.Regions is null || r.Regions.Any(region => !RegionValid(region))
                || r.GroundCell is not null || r.Id.StartsWith(InventoryGround.Prefix, StringComparison.Ordinal)
                || r.Regions.Select(region => region.Id).Distinct().Count() != r.Regions.Length || r.MaxMassGrams is <= 0 or > 100000
                || (r.ActorId is null) == (r.PropId is null) || r.ActorId is not null && !world.Actors.Any(a => a.Id == r.ActorId)
                || r.PropId is not null && !world.Map.Definition.Props.Any(p => p.Id == r.PropId))
            || c.Roots.Any(r => r.BodyFamily is not null && !state.BodyFits!.Content.Bodies.Any(b => b.Id == r.BodyFamily))
            || c.Roots.Select(r => r.Id).Distinct().Count() != c.Roots.Length
            || c.Items.Any(i => i is null || !Id(i.Id) || i.Id.StartsWith(InventoryGround.Prefix, StringComparison.Ordinal) || !c.Definitions.Any(d => d.Id == i.DefinitionId)
                || c.Roots.Any(r => r.Id == i.Id)) || c.Items.Select(i => i.Id).Distinct().Count() != c.Items.Length)
            throw new ArgumentException("Invalid inventory content.");
        foreach (var definition in c.Definitions)
        {
            var shape = definition.Wear?.FitProfile is { } id ? state.BodyFits!.Shape(id) : null;
            if (definition.Wear is not null && (shape is null || shape.Coverage?.Closed is { Length: 0 }))
                throw new ArgumentException("Wearable equipment requires a nonempty physical body footprint.");
            var cells = shape is null ? null : BodyFitGeometry.Cells(shape.Rows)
                .Select(cell => new BodyHex(cell.Q - shape.DefaultPose.Q, cell.R - shape.DefaultPose.R)).ToHashSet();
            foreach (var region in definition.Regions ?? [])
                if (region.AccessFootprint is { } masks && (cells is null || masks.Length == 0 || !BodyFitCatalog.SurfaceMasksValid(masks, cells)))
                    throw new ArgumentException("Invalid compartment access footprint: " + definition.Id + "/" + region.Id);
            var mounted = shape?.Coverage is { } cover && cover.Closed.Concat(cover.Open).Any(m => m.Surface == "mounted")
                || definition.Regions?.Any(r => r.AccessFootprint?.Any(m => m.Surface == "mounted") == true) == true
                || definition.Wear?.FasteningFootprint?.Any(m => m.Surface == "mounted") == true;
            if (definition.Wear is { } wear && (wear.OverlayLimit is not null && shape?.Coverage?.Closed is not { Length: > 0 }
                || wear.FasteningFootprint is { } fastening && (cells is null || fastening.Length == 0 || !BodyFitCatalog.SurfaceMasksValid(fastening, cells))))
                throw new ArgumentException("Overlay/fastening geometry requires valid authored body masks.");
            if (mounted && definition.Wear!.Mounts.Any(m => m.BodySurface is null))
                throw new ArgumentException("Mounted surface masks require a surface on every worn mount.");
        }
        InventorySaveCodec.ValidateGraph(state);
    }
}
