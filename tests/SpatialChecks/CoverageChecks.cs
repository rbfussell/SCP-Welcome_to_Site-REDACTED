using SiteSimulation;

internal static class CoverageChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Coverage: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        void Finish(SpatialSimulation sim)
        {
            for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) sim.Tick();
            Check(sim.Inventory.Pending is null, "handling finishes in bounded local time");
        }
        void Act(SpatialSimulation sim, InventoryRequest request) { Check(sim.HandleInventory(request), sim.LastMessage); Finish(sim); }
        InventoryRequest Move(string id, string owner, string region) => new(InventoryActionKind.Move, id, new(owner, region));
        InventoryRequest Coat(bool open) => new(open ? InventoryActionKind.OpenGarment : InventoryActionKind.CloseGarment, "avatar-coat");
        var sim = Sim();
        var pockets = new[] { "left", "right", "cargo-left", "cargo-right" };
        Check(sim.Inventory.Definition("avatar-trousers").Regions!.Take(4).Select(r => r.Id).SequenceEqual(pockets) && sim.Inventory.Definition("avatar-trousers").Regions!.Length == 6, "four front and two rear regions belong to one trouser item");
        foreach (var id in pockets)
        {
            var coverage = sim.InventoryPocketCoverage("avatar-trousers", id)!;
            Check(!coverage.Covered && coverage.Mouth.Length == 2 && coverage.Blocked.Length == 0 && !coverage.CoarseFallback,
                "open jacket clears each authored front mouth: " + id);
            Check(sim.InventoryAccess("avatar-trousers", id).Allowed, "uncovered open compartment is reachable: " + id);
        }
        var before = sim.Save();
        Check(sim.InventoryPocketCoverage("observer-trousers", "left") is null && sim.InventoryPocketCoverage("avatar-trousers", "absent") is null
            && sim.Save() == before, "coverage inspection neither reveals other people's pockets nor changes state");
        Check(sim.HandleInventory(Coat(false)), "begin closing coat"); sim.Tick();
        Check(sim.InventoryAccess("avatar-trousers", "left").Allowed, "coverage waits until physical zipper work completes");
        var mid = sim.Save(); var copy = Sim(); copy.Restore(mid); Finish(sim); Finish(copy);
        Check(sim.Save() == copy.Save(), "pending closure resumes with identical coverage and clocks");
        foreach (var id in pockets)
        {
            var coverage = sim.InventoryPocketCoverage("avatar-trousers", id)!;
            var hip = id is "left" or "right";
            Check(coverage.Covered == hip && sim.InventoryAccess("avatar-trousers", id).Allowed != hip, "geometric cover distinguishes hip from cargo: " + id);
            Check(coverage.Blocked.Length == (hip ? 1 : 0), "one covered cell of a two-cell mouth blocks access, without covering the whole garment");
            Check(coverage.Blockers.SequenceEqual(hip ? ["avatar-coat"] : Array.Empty<string>()), "blocker identity comes from actual intersecting outer gear");
        }
        Check(sim.InventoryContents("avatar-trousers", "left").Count == 0 && sim.InventoryContents("gum-packet", "pieces").Count == 0,
            "cover applies through the complete nested access path");
        Check(!sim.OscpAccess(OscpDevice.Phone).Available, "ready equipment still requires its real pocket mouth to be accessible");
        Check(!sim.QuoteInventory(Move("gum-packet", "person:avatar", "hand")).Allowed
            && !sim.QuoteInventory(new(InventoryActionKind.Arrange, "avatar-trousers", new("avatar-trousers", "left"))).Allowed,
            "manual movement and automatic packing share geometric access gates");
        Check(!sim.QuoteInventory(new(InventoryActionKind.Close, "avatar-trousers", RegionId: "left")).Allowed,
            "closing a covered pocket cannot bypass the same access gate");
        var child = sim.Inventory.Items["gum-1"].Placement;
        Act(sim, Move("flashlight", "avatar-trousers", "cargo-left"));
        Check(sim.InventoryContents("avatar-trousers", "cargo-left").Single().Id == "flashlight" && sim.Inventory.Items["gum-1"].Placement == child,
            "cargo storage functions under the closed coat without disturbing hidden nested contents");
        Act(sim, new(InventoryActionKind.Close, "avatar-trousers", RegionId: "cargo-left"));
        Check(!sim.InventoryAccess("avatar-trousers", "cargo-left").Allowed && !sim.InventoryPocketCoverage("avatar-trousers", "cargo-left")!.Covered,
            "own closed flap and outer coverage are independent reasons for inaccessibility");
        Check(sim.Inventory.IsOpen("avatar-trousers", "right"), "outer cover never changes the underlying pocket's own closure state");
        Act(sim, new(InventoryActionKind.Open, "avatar-trousers", RegionId: "cargo-left"));
        Act(sim, Coat(true));
        Check(pockets.All(id => sim.InventoryAccess("avatar-trousers", id).Allowed) && !sim.InventoryMoveAccess("avatar-trousers").Allowed,
            "opening exposes mouths while retaining independent removal restraints");
        var restored = Sim(); restored.Restore(sim.Save());
        Check(restored.Save() == sim.Save() && pockets.All(id => !restored.InventoryPocketCoverage("avatar-trousers", id)!.Covered), "all compartment states and effective cover persist");
        Act(sim, Move("field-phone", "avatar-trousers", "cargo-right")); Act(sim, Coat(false));
        Check(sim.OscpAccess(OscpDevice.Phone).Available, "the same handset in an uncovered ready cargo pocket remains usable");
        Act(sim, Coat(true));
        Check(sim.HandleInventory(Move("gum-packet", "person:avatar", "hand")), "begin extraction before unexpected cover change");
        sim.Inventory.Items["avatar-coat"] = sim.Inventory.Items["avatar-coat"] with { GarmentOpen = false };
        Finish(sim);
        Check(sim.Inventory.Items["gum-packet"].Placement.ParentId == "avatar-trousers", "completion rechecks actual coverage and aborts without relocating a newly blocked source");

        // Independent source vectors: rotate the same local mouth and cover, and resolve mounted front/back surfaces.
        var masks = new[] { new BodySurfaceMask("mounted", [new(1, 0)]) };
        var projected = InventoryCoverage.Project(masks, new(2, 3, 1), new("back", ["torso-back"], "back"));
        Check(projected.Single() == new BodySurfaceCell("back", new(2, 4)), "surface and position use the worn pose, including sixth-turn rotation");
        Check(InventoryCoverage.Project(masks, new(2, 3, 1), new("front", ["torso-front"], "front")).Single().Surface == "front",
            "the same attachment/pack geometry can resolve on the opposite body surface");
        var world = new SimulationWorld(map, config); var state = world.Inventory;
        state.Items["avatar-coat"] = state.Items["avatar-coat"] with { GarmentOpen = false };
        var shapes = state.BodyFits!.Content.Shapes; var coatIndex = Array.FindIndex(shapes, s => s.Id == "coat");
        var original = shapes[coatIndex];
        shapes[coatIndex] = original with { Coverage = new(original.Coverage!.Closed.Where(m => m.Surface == "back").ToArray(), original.Coverage.Open.Where(m => m.Surface == "back").ToArray()) };
        Check(InventoryAccessService.Placement(world, "avatar", new("avatar-trousers", "left")).Allowed,
            "matching hex coordinates on the back cannot block a front pocket; mapped gear does not fall back to coarse labels");
        shapes[coatIndex] = original;
        var defs = state.Content!.Definitions; var trouserIndex = Array.FindIndex(defs, d => d.Id == "trousers"); var trousers = defs[trouserIndex];
        defs[trouserIndex] = trousers with { Regions = trousers.Regions!.Select(r => r.Id == "left" ? r with
            { AccessFootprint = r.AccessFootprint!.Select(m => m with { Surface = "back" }).ToArray() } : r).ToArray() };
        state.Items["avatar-coat"] = state.Items["avatar-coat"] with { GarmentOpen = true };
        Check(!InventoryAccessService.Placement(world, "avatar", new("avatar-trousers", "left")).Allowed,
            "opening the front leaves the authored back cover in place");
        defs[trouserIndex] = trousers;
        shapes[coatIndex] = original with { Coverage = null };
        state.Items["avatar-coat"] = state.Items["avatar-coat"] with { GarmentOpen = false };
        var fallback = InventoryCoverage.Evaluate(state, state.Items["avatar-trousers"], state.Region("avatar-trousers", "left"))!;
        Check(fallback.Covered && fallback.CoarseFallback && fallback.Blocked.Length == 0, "unmapped outer gear conservatively keeps the old coverage rule without fabricating overlap cells");
        shapes[coatIndex] = original;
        var ci = Array.FindIndex(defs, d => d.Id == "coat"); var coat = defs[ci];
        defs[ci] = coat with { Wear = coat.Wear! with { CoversAccess = false, BlocksRemoval = true } };
        Check(InventoryAccessService.Placement(world, "avatar", new("avatar-trousers", "left")).Allowed
            && !InventoryActions.Validate(world, "avatar", Move("avatar-trousers", "person:avatar", "hand"), out _, out _, out _),
            "non-covering removal restraints retain the confirmed independent flag behavior");
        var pack = Sim(); pack.Player.Cell = new(13, 6);
        Check(!pack.InventoryPocketCoverage("avatar-coat", "chest")!.Covered, "back-worn pack does not occlude the front chest pocket");
        Act(pack, Move("spare-backpack", "person:avatar", "front"));
        var chest = pack.InventoryPocketCoverage("avatar-coat", "chest")!;
        Check(chest.Covered && chest.Blockers.SequenceEqual(["spare-backpack"]), "front-worn pack blocks the chest mouth by actual same-surface overlap");
        Act(pack, Move("spare-backpack", "person:avatar", "hand"));
        Check(!pack.InventoryPocketCoverage("avatar-coat", "chest")!.Covered, "carried gear does not project worn coverage");

        void RejectShape(BodyFitShape shape)
        {
            var content = state.BodyFits.Content;
            try { BodyFitCatalog.Validate(content with { Shapes = [shape] }); throw new Exception("Invalid cover metadata accepted"); }
            catch (ArgumentException) { passed++; }
        }
        RejectShape(original with { Coverage = new([new("sideways", [new(0, 0)])], []) });
        RejectShape(original with { Coverage = new([new("front", [new(999, 999)])], []) });
        RejectShape(original with { Coverage = new([], original.Coverage!.Closed) });
        RejectShape(original with { Coverage = new([original.Coverage!.Closed[0], original.Coverage.Closed[0]], []) });
        var malformed = new SimulationWorld(map, config); var md = malformed.Inventory.Content!.Definitions;
        var mi = Array.FindIndex(md, d => d.Id == "trousers"); var mt = md[mi];
        md[mi] = mt with { Regions = mt.Regions!.Select(r => r.Id == "left" ? r with { AccessFootprint = [new("front", [new(999, 999)])] } : r).ToArray() };
        try { InventoryContentValidation.Validate(malformed); throw new Exception("Invalid pocket footprint accepted"); }
        catch (ArgumentException) { Check(true, "pocket footprints must be registered on their owner's worn shape"); }
        Console.WriteLine($"COVERAGE_CHECKS_PASS: {passed} assertions; geometric mouths, surfaces, paid access, nested gates, compatibility and persistence.");
        return passed;
    }
}
