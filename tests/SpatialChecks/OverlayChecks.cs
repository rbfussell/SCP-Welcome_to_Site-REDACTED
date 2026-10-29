using SiteSimulation;
using System.Text.Json.Nodes;

internal static class OverlayChecks
{
    private static readonly string[] Belts = ["utility-belt", "utility-belt-2", "utility-belt-3", "utility-belt-4"];
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Overlays: " + reason); passed++; }
        SpatialSimulation Sim() { var s = new SpatialSimulation(map, config); s.Player.Cell = new(13, 6); return s; }
        void Finish(SpatialSimulation s) { for (var i = 0; i < 2000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "paid work resolves"); }
        void Act(SpatialSimulation s, InventoryRequest request) { Check(s.HandleInventory(request), s.LastMessage); Finish(s); }
        InventoryRequest Fit(string id, BodyPose pose) => new(InventoryActionKind.Move, id, new("person:avatar", "belt", BodyPose: pose));
        InventoryRequest Mount(int q) => new(InventoryActionKind.Move, "utility-pouch", new("utility-belt", "webbing", AttachmentPose: new(q, 0)));
        void ClearPack(SpatialSimulation s) => Act(s, new(InventoryActionKind.Move, "avatar-backpack", new("fixture:crate-a", "store", 0, 8)));
        var waist = new BodyPose(-5, 11); var diagonal = new BodyPose(-3, 9, 2); var chest = new BodyPose(-4, 9);
        var sim = Sim(); ClearPack(sim);
        Act(sim, Fit(Belts[0], waist)); Act(sim, Fit(Belts[1], diagonal)); Act(sim, Fit(Belts[2], chest));
        var cells = sim.InventoryWornCells;
        foreach (var side in new[] { "front", "back" })
        {
            var atWaist = cells.Where(c => c.Cell == new BodySurfaceCell(side, new(-5, 11))).ToArray();
            Check(atWaist.Select(c => c.ItemId).SequenceEqual(new[] { "avatar-trousers", "avatar-coat", Belts[0], Belts[1] }),
                "independent native crossing contains deeper trousers, one base jacket and two belts: " + side);
            Check(atWaist[^1] is { Layer: 1, SubLayer: 2, BandCount: 3 }, "deeper garment does not count twice in the current band: " + side);
        }
        Check(cells.Where(c => Belts.Take(3).Contains(c.ItemId)).Max(c => c.BandCount) == 3,
            "three worn belts fit where their separate crossings never exceed two overlaid straps");
        var before = sim.Save();
        Check(!sim.QuoteInventory(Fit(Belts[3], chest)).Allowed && !sim.HandleInventory(Fit(Belts[3], chest)) && sim.Save() == before,
            "a fourth-item intersection rejects with no physical, timing or AP mutation");
        Check(!sim.QuoteInventory(new(InventoryActionKind.Move, Belts[0], new("person:avatar", "hand"))).Allowed,
            "a lower strap cannot be pulled through a physically intersecting upper strap");
        Check(!sim.QuoteInventory(new(InventoryActionKind.CloseGarment, "avatar-coat")).Allowed,
            "a strap across the zipper independently prevents operating that fastening");
        Check(!sim.QuoteInventory(Mount(0)).Allowed && sim.Save() == before,
            "a lower belt's covered required contact cannot receive a pouch");
        Check(sim.QuoteInventory(Mount(1)).Allowed, "another exposed contact on the same lower belt remains usable");
        Check(sim.HandleInventory(Mount(1)), sim.LastMessage); sim.Tick();
        var midway = sim.Save(); var copy = Sim(); copy.Restore(midway);
        Check(copy.Save() == midway, "pending mount on a layered provider restores exactly");
        Finish(sim); Finish(copy); Check(sim.Save() == copy.Save(), "layered provider work resumes identically");
        var restored = Sim(); restored.Restore(sim.Save());
        Check(restored.InventoryWornCells.SequenceEqual(sim.InventoryWornCells), "derived bands and subpositions reproduce from order and body pose");

        var occupied = Sim(); ClearPack(occupied); Act(occupied, Fit(Belts[0], waist)); Act(occupied, Mount(0));
        before = occupied.Save();
        Check(!occupied.HandleInventory(Fit(Belts[1], diagonal)) && occupied.LastMessage.Contains("contact", StringComparison.OrdinalIgnoreCase)
            && occupied.Save() == before, "a proposed overlay cannot cover an occupied contact or silently detach its child");
        // A scenario can begin with a belt underneath an open jacket. Closing it must revalidate existing children.
        var closure = Sim(); var state = closure.Inventory;
        foreach (var item in state.Items.Values.Where(i => i.Placement.WornOrder is not null).ToArray())
            state.Items[item.Id] = item with { Placement = item.Placement with { WornOrder = item.Placement.WornOrder * 10 } };
        state.Items[Belts[0]] = state.Items[Belts[0]] with { Placement = new("person:avatar", "belt", BodyPose: waist, WornOrder: 45) };
        InventoryPlacement.Validate(state); Act(closure, Mount(0)); before = closure.Save();
        Check(!closure.HandleInventory(new(InventoryActionKind.CloseGarment, "avatar-coat")) && closure.Save() == before,
            "closing clothing also refuses to cover an occupied mounting contact");

        // Fully authored symmetric reference body: both diagonals fit, independent of the deliberately crude native silhouette.
        foreach (var bare in new[] { false, true })
        {
            var reference = Reference(map, config, bare); state = reference.Inventory;
            var center = new BodyPose(-3, 7);
            var start = bare ? 1 : 2;
            var allowed = bare ? 3 : 2;
            for (var i = 0; i < allowed; i++) state.Items[Belts[i]] = state.Items[Belts[i]] with
                { Placement = new("person:avatar", "belt", BodyPose: center with { Rotation = i + 1 }, WornOrder: start + i) };
            InventoryPlacement.Validate(state);
            var stack = InventoryWornStack.Build(state, "person:avatar");
            foreach (var side in new[] { "front", "back" })
            {
                var crossing = stack.Where(c => c.Cell == new BodySurfaceCell(side, new(-3, 7))).ToArray();
                Check(crossing[^1] is { Layer: 0, SubLayer: 2, BandCount: 3 }, "symmetric X uses base once, or three naked straps: " + side);
            }
            state.Items[Belts[allowed]] = state.Items[Belts[allowed]] with
                { Placement = new("person:avatar", "belt", BodyPose: center, WornOrder: start + allowed) };
            try { InventoryPlacement.Validate(state); throw new Exception("Overfull reference crossing accepted"); }
            catch (ArgumentException ex) { Check(ex.Message.Contains("Overlay limit"), "four items in the same local band fail"); }
        }
        // Every overlay's allowance matters; a later permissive belt cannot bypass an earlier tighter strap.
        var mixed = Reference(map, config, false); state = mixed.Inventory;
        var di = Array.FindIndex(state.Content!.Definitions, d => d.Id == "dressing");
        var beltWear = state.Content.Definitions.Single(d => d.Id == "utility-belt").Wear!;
        state.Content.Definitions[di] = state.Content.Definitions[di] with { Wear = beltWear with { OverlayLimit = 2 } };
        var original = state.Content.Items.Single(i => i.Id == "spare-dressing");
        state.Items[original.Id] = original with { Placement = new("person:avatar", "belt", BodyPose: new(-3, 7), WornOrder: 2) };
        state.Items[Belts[0]] = state.Items[Belts[0]] with { Placement = new("person:avatar", "belt", BodyPose: new(-3, 7), WornOrder: 3) };
        try { InventoryPlacement.Validate(state); throw new Exception("Earlier strap limit bypassed"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("Overlay limit"), "all applicable overlay allowances remain binding"); }

        var separate = Reference(map, config, true); state = separate.Inventory;
        var shapes = state.BodyFits!.Content.Shapes; var bsi = Array.FindIndex(shapes, s => s.Id == "belt");
        var front = shapes[bsi] with { Coverage = new(shapes[bsi].Coverage!.Closed.Where(m => m.Surface == "front").ToArray(),
            shapes[bsi].Coverage!.Open.Where(m => m.Surface == "front").ToArray()) }; shapes[bsi] = front;
        var ssi = Array.FindIndex(shapes, s => s.Id == "shirt");
        shapes[ssi] = front with { Id = "shirt", Coverage = new(front.Coverage!.Closed.Select(m => m with { Surface = "back" }).ToArray(),
            front.Coverage.Open.Select(m => m with { Surface = "back" }).ToArray()) };
        var defs2 = state.Content!.Definitions; var bdi = Array.FindIndex(defs2, d => d.Id == "utility-belt");
        defs2[bdi] = defs2[bdi] with { Wear = defs2[bdi].Wear! with { OverlayLimit = 1 } };
        var ddi = Array.FindIndex(defs2, d => d.Id == "dressing");
        defs2[ddi] = defs2[ddi] with { Wear = defs2[bdi].Wear! with { FitProfile = "shirt" } };
        state.Items[Belts[0]] = state.Items[Belts[0]] with { Placement = new("person:avatar", "belt", BodyPose: new(-3, 7), WornOrder: 1) };
        state.Items[original.Id] = original with { Placement = new("person:avatar", "belt", BodyPose: new(-3, 7), WornOrder: 2) };
        InventoryPlacement.Validate(state);
        Check(InventoryWornStack.Build(state, "person:avatar").All(c => c.BandCount == 1 && c.SubLayer == 0),
            "identical coordinates on opposite surfaces do not consume each other's allowance");
        state.Items[Belts[1]] = state.Items[Belts[1]] with { Placement = new("person:avatar", "belt", BodyPose: new(-3, 7), WornOrder: 3) };
        try { InventoryPlacement.Validate(state); throw new Exception("Same-surface limit bypassed"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("Overlay limit"), "the same front surface still enforces its own limit"); }
        var bulkWorld = Reference(map, config, true); state = bulkWorld.Inventory;
        var db = state.Content!.Definitions; bdi = Array.FindIndex(db, d => d.Id == "utility-belt");
        db[bdi] = db[bdi] with { Wear = db[bdi].Wear! with { Fit = db[bdi].Wear!.Fit! with { MaxUnderBulk = 0 } } };
        for (var i = 0; i < 2; i++) state.Items[Belts[i]] = state.Items[Belts[i]] with
            { Placement = new("person:avatar", "belt", BodyPose: new(-3, 7), WornOrder: i + 1) };
        try { InventoryPlacement.Validate(state); throw new Exception("Bulk allowance bypassed"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("clearance"), "geometric band capacity never replaces the independent bulk allowance"); }
        var restraintWorld = Reference(map, config, true); state = restraintWorld.Inventory;
        var rd = state.Content!.Definitions; bdi = Array.FindIndex(rd, d => d.Id == "utility-belt");
        rd[bdi] = rd[bdi] with { Wear = rd[bdi].Wear! with { RemovalRegions = ["feet"] } };
        for (var i = 0; i < 2; i++) state.Items[Belts[i]] = state.Items[Belts[i]] with
            { Placement = new("person:avatar", "belt", BodyPose: new(-3, 7), WornOrder: i + 1) };
        Check(!InventoryActions.Validate(restraintWorld, "avatar", new(InventoryActionKind.Move, Belts[0], new("person:avatar", "hand")), out _, out _, out _),
            "intersecting straps restrain each other by geometry even when legacy region labels differ");

        var tactical = Sim(); ClearPack(tactical); Act(tactical, Fit(Belts[0], waist)); Act(tactical, Fit(Belts[1], diagonal));
        Act(tactical, new(InventoryActionKind.Move, Belts[2], new("person:avatar", "hand")));
        tactical.Player.Cell = new(9, 2); tactical.Actors.Single(a => a.Id == "tech").Cell = new(10, 2); tactical.Face(new Hex(10, 2).World);
        Check(tactical.ArmTraining() && tactical.Combat.Active, "actual tactical fixture starts");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var turn = tactical.Combat.Cursor; var quote = tactical.QuoteInventory(Fit(Belts[2], chest));
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = quote.Ap - 1 }; before = tactical.Save();
        Check(!tactical.HandleInventory(Fit(Belts[2], chest)) && tactical.Save() == before, "a valid overlay still requires sufficient AP");
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = 10 };
        Check(tactical.HandleInventory(Fit(Belts[2], chest)) && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - quote.Ap,
            "an overlay buys normal handling and spends its AP once"); Finish(tactical);
        Check(tactical.InventoryWornCells.Any(c => c.ItemId == Belts[2] && c.SubLayer == 2), "tactical overlay commits into the expected band");
        var good = sim.Save(); var bad = JsonNode.Parse(good)!;
        var node = bad["Inventory"]!["Items"]!.AsArray().Single(n => n!["Id"]!.GetValue<string>() == Belts[3])!;
        node["Placement"] = new JsonObject { ["ParentId"] = "person:avatar", ["RegionId"] = "belt", ["WornOrder"] = 100,
            ["BodyPose"] = new JsonObject { ["Q"] = chest.Q, ["R"] = chest.R, ["Rotation"] = 0 } };
        var victim = Sim(); before = victim.Save();
        try { victim.Restore(bad.ToJsonString()); throw new Exception("Overfull saved band accepted"); }
        catch (ArgumentException) { Check(victim.Save() == before, "overfull saved bands reject atomically"); }
        foreach (var limit in new[] { 0, 17 })
        {
            var invalid = new SimulationWorld(map, config); var defs = invalid.Inventory.Content!.Definitions;
            var index = Array.FindIndex(defs, d => d.Id == "utility-belt"); defs[index] = defs[index] with { Wear = defs[index].Wear! with { OverlayLimit = limit } };
            try { InventoryContentValidation.Validate(invalid); throw new Exception("Invalid authored overlay limit accepted"); }
            catch (ArgumentException) { Check(true, "authored overlay allowance is bounded"); }
        }
        var empty = new SimulationWorld(map, config); var es = empty.Inventory.BodyFits!.Content.Shapes;
        var ei = Array.FindIndex(es, s => s.Id == "belt"); es[ei] = es[ei] with { Coverage = new([], []) };
        try { InventoryContentValidation.Validate(empty); throw new Exception("Empty overlay footprint accepted"); }
        catch (ArgumentException) { Check(true, "an overlay cannot hide its occupancy with empty masks"); }
        var unmapped = new SimulationWorld(map, config); var unmappedDefs = unmapped.Inventory.Content!.Definitions;
        var unmappedIndex = Array.FindIndex(unmappedDefs, d => d.Id == "shirt");
        unmappedDefs[unmappedIndex] = unmappedDefs[unmappedIndex] with { Wear = unmappedDefs[unmappedIndex].Wear! with { FitProfile = null } };
        try { InventoryContentValidation.Validate(unmapped); throw new Exception("Missing physical shape accepted"); }
        catch (ArgumentException) { Check(true, "ordinary clothing cannot bypass physical stack fit by omitting its shape"); }
        var unbound = new SimulationWorld(map, config); var ud = unbound.Inventory.Content!.Definitions;
        var ui = Array.FindIndex(ud, d => d.Id == "coat"); ud[ui] = ud[ui] with
            { Wear = ud[ui].Wear! with { FasteningFootprint = [new("mounted", [new(0, 0)])] } };
        try { InventoryContentValidation.Validate(unbound); throw new Exception("Unbound fastening surface accepted"); }
        catch (ArgumentException) { Check(true, "mounted fastening geometry requires an explicit mount surface"); }
        var zones = new SimulationWorld(map, config).Inventory.BodyFits!.Content;
        var body = zones.Bodies[0]; var sourceZones = body.Zones!;
        try { BodyFitCatalog.Validate(zones with { Bodies = [body with { Zones = sourceZones.Append(sourceZones[0]).ToArray() }] }); throw new Exception("Overlapping body zones accepted"); }
        catch (ArgumentException) { Check(true, "anatomical zones cannot assign two regions to the same surface cell"); }
        Console.WriteLine($"OVERLAY_CHECKS_PASS: {passed} assertions; local bands, symmetric X, naked/clothed limits, exposed contacts, closures, paid AP and persistence.");
        return passed;
    }

    /// <summary>Isolated authored geometry fixture, not a saved native campaign with missing identities.</summary>
    private static SimulationWorld Reference(string map, string config, bool bare)
    {
        var world = new SimulationWorld(map, config); var state = world.Inventory; var catalog = state.BodyFits!;
        var center = new BodyHex(-3, 7);
        var cells = Enumerable.Range(-2, 5).SelectMany(q => Enumerable.Range(-2, 5).Where(r => Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(q + r))) <= 2)
            .Select(r => center + new BodyHex(q, r))).ToArray();
        string[] Rows(IEnumerable<BodyHex> points) => Enumerable.Range(0, 20).Select(r => new string(Enumerable.Range(0, 9)
            .Select(c => points.Contains(new BodyHex(c - 4 - r / 2, r)) ? '#' : '.').ToArray())).ToArray();
        var body = catalog.Content.Bodies[0] with { Rows = Rows(cells) }; catalog.Content.Bodies[0] = body;
        var bi = Array.FindIndex(catalog.Content.Shapes, s => s.Id == "belt");
        var belt = catalog.Content.Shapes[bi] with { Rows = Rows(Enumerable.Range(-2, 5).Select(q => center + new BodyHex(q, 0))), DefaultPose = new(-3, 7) };
        catalog.Content.Shapes[bi] = belt;
        var si = Array.FindIndex(catalog.Content.Shapes, s => s.Id == "shirt");
        var masks = new[] { "front", "back" }.Select(side => new BodySurfaceMask(side, cells.Select(c => new BodyHex(c.Q - center.Q, c.R - center.R)).ToArray())).ToArray();
        var shirt = catalog.Content.Shapes[si] with { Rows = Rows(cells), DefaultPose = new(-3, 7), Contacts = [], Coverage = new(masks, masks) };
        catalog.Content.Shapes[si] = shirt;
        BodyFitCatalog.Validate(catalog.Content with { Bodies = [body], Shapes = [belt, shirt] });
        foreach (var id in state.Items.Keys.Where(id => !Belts.Contains(id) && (bare || id != "avatar-shirt")).ToArray()) state.Items.Remove(id);
        if (!bare) state.Items["avatar-shirt"] = state.Items["avatar-shirt"] with { Placement = new("person:avatar", "torso", WornOrder: 1) };
        return world;
    }
}
