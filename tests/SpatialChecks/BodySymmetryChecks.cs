using SiteSimulation;

internal static class BodySymmetryChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Body cleanup: " + reason); passed++; }
        var sim = new SpatialSimulation(map, config); sim.Player.Cell = new(13, 6);
        var state = sim.Inventory; var catalog = state.BodyFits!; var body = catalog.Body("human-nine-shoulder");
        static BodyHex Mirror(BodyHex c) => new(-c.Q - c.R, c.R);
        var bodyCells = BodyFitGeometry.Cells(body.Rows).ToHashSet();
        Check(bodyCells.All(c => bodyCells.Contains(Mirror(c))), "every body cell has a vertical mirror, including odd rows");
        var visited = new HashSet<BodyHex>(); var pending = new Queue<BodyHex>(); pending.Enqueue(bodyCells.First());
        while (pending.TryDequeue(out var cell))
            if (visited.Add(cell)) foreach (var next in Enumerable.Range(0, 6).Select(cell.Neighbor).Where(bodyCells.Contains).Where(c => !visited.Contains(c))) pending.Enqueue(next);
        Check(visited.SetEquals(bodyCells), "head, torso, arms and legs remain one connected body");
        foreach (var shape in catalog.Content.Shapes)
        {
            var fit = BodyFitGeometry.Evaluate(body, shape, shape.DefaultPose);
            Check(fit.Cells.All(c => fit.Cells.Contains(Mirror(c))), "default worn mask mirrors: " + shape.Id);
            Check(fit.Contacts.All(c => fit.Contacts.Contains(new(Mirror(c.Cell), 5 - c.Direction))),
                "boundary edges mirror with the silhouette: " + shape.Id);
        }
        var left = sim.InventoryPocketCoverage("avatar-trousers", "left")!.Mouth;
        var right = sim.InventoryPocketCoverage("avatar-trousers", "right")!.Mouth;
        Check(left.Select(c => c with { Cell = Mirror(c.Cell) }).ToHashSet().SetEquals(right), "hip mouths are exact mirrors");
        Check(sim.InventoryPocketCoverage("avatar-trousers", "cargo-left")!.Mouth.Select(c => c with { Cell = Mirror(c.Cell) }).ToHashSet()
            .SetEquals(sim.InventoryPocketCoverage("avatar-trousers", "cargo-right")!.Mouth), "cargo mouths are exact mirrors");
        var belt = catalog.Shape("belt"); var anchor = new BodyPose(-4, 8);
        var one = BodyFitGeometry.Evaluate(body, belt, anchor with { Rotation = 1 });
        var two = BodyFitGeometry.Evaluate(body, belt, anchor with { Rotation = 2 });
        Check(one.Fits && two.Fits && one.Cells.Length == 10 && two.Cells.Length == 10, "native chest X uses authored ten-cell span");
        Check(one.Cells.ToHashSet().SetEquals(new[] { new BodyHex(-4, 4), new(-4, 5), new(-4, 6), new(-4, 7), new(-4, 8),
            new(-4, 9), new(-4, 10), new(-4, 11), new(-4, 12), new(-4, 13) }), "independent first diagonal endpoints and full run");
        Check(one.Cells.Select(Mirror).ToHashSet().SetEquals(two.Cells), "second diagonal mirrors the first instead of a shifted V");
        Check(one.Cells.Intersect(two.Cells).SequenceEqual(new[] { new BodyHex(-4, 8) }), "diagonals cross once at the chosen chest anchor");
        Check(!BodyFitGeometry.Evaluate(body, belt with { LinearSpan = new(5, 9) }, anchor with { Rotation = 1 }).Fits,
            "insufficient authored length cannot fasten, even when its core is inside the body");
        Check(!BodyFitGeometry.Evaluate(body, belt, new(-6, 12, 1)).Fits, "adjustment cannot bridge the gap between legs");
        for (var rotation = 0; rotation < 6; rotation++)
        {
            var result = BodyFitGeometry.Evaluate(body, belt, anchor with { Rotation = rotation });
            Check(result.Fits && result.FailedContacts.Length == 0 && result.Cells.All(bodyCells.Contains), "all six axes/orientations have real boundary contacts");
        }
        void Finish(SpatialSimulation s) { for (var i = 0; i < 2000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "normal paid work resolves"); }
        void Act(InventoryRequest request) { Check(sim.HandleInventory(request), sim.LastMessage); Finish(sim); }
        InventoryRequest Fit(string id, int rotation) => new(InventoryActionKind.Move, id, new("person:avatar", "belt", BodyPose: anchor with { Rotation = rotation }));
        Act(new(InventoryActionKind.Move, "avatar-backpack", new("fixture:crate-a", "store", 0, 8)));
        Act(Fit("utility-belt", 1));
        Check(state.Items["utility-belt"].Placement.BodyPose == anchor with { Rotation = 1 }, "paid placement retains chosen anchor");
        foreach (var side in new[] { "front", "back" })
            Check(InventoryWornStack.Cells(state, state.Items["utility-belt"]).Where(c => c.Surface == side).Select(c => c.Cell).ToHashSet().SetEquals(one.Cells),
                "wear bands and exposure use extended geometry on " + side);
        Check(sim.HandleInventory(Fit("utility-belt-2", 2)), sim.LastMessage); sim.Tick();
        var saved = sim.Save(); var clone = new SpatialSimulation(map, config); clone.Restore(saved);
        Check(clone.Save() == saved, "pending adjustable fit restores exactly without a second length state");
        Finish(sim); Finish(clone); Check(sim.Save() == clone.Save(), "adjustable X resumes identically");
        foreach (var side in new[] { "front", "back" })
            Check(sim.InventoryWornCells.Last(c => c.Cell == new BodySurfaceCell(side, new(-4, 8))) is { SubLayer: 2, BandCount: 3 },
                "native X counts one base and two straps on " + side);
        var before = sim.Save();
        Check(!sim.HandleInventory(Fit("utility-belt-3", 0)) && sim.Save() == before && sim.LastMessage.Contains("Overlay limit"),
            "third strap at clothed X intersection fails atomically");
        Check(!sim.QuoteInventory(new(InventoryActionKind.Move, "utility-pouch", new("utility-belt", "webbing", AttachmentPose: new(0, 0)))).Allowed,
            "other diagonal covers central attachment contact using fitted cells");
        foreach (var span in new[] { new BodyLinearSpan(0, 10), new(5, 4), new(5, 34), new(4, 10) })
        {
            try { BodyFitCatalog.Validate(catalog.Content with { Shapes = [belt with { LinearSpan = span }] }); throw new Exception("Invalid span accepted"); }
            catch (ArgumentException) { Check(true, "invalid authored range is rejected"); }
        }
        try { BodyFitCatalog.Validate(catalog.Content with { Shapes = [catalog.Shape("shirt") with { LinearSpan = new(5, 10) }] }); throw new Exception("Nonlinear shirt accepted"); }
        catch (ArgumentException) { Check(true, "span authoring is limited to straight, fully covered strips"); }
        Console.WriteLine($"BODY_SYMMETRY_CHECKS_PASS: {passed} assertions; mirrored masks/mouths, connected body, bounded spans, native X, paid work and continuation.");
        return passed;
    }
}
