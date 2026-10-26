using SiteSimulation;
using System.Text.Json.Nodes;

internal static class BodyFitChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("Body fit: " + message); passed++; }
        SpatialSimulation Sim() => new(map, config);
        void Finish(SpatialSimulation sim)
        {
            for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) sim.Tick();
            Check(sim.Inventory.Pending is null, "paid fitting resolves");
        }
        var sim = Sim(); var catalog = sim.Inventory.BodyFits!;
        var body = catalog.Body("human-nine-shoulder");
        Check(body.Rows[4].Count(c => c == '#') == 9, "fixture really has nine shoulder cells");
        foreach (var shape in catalog.Content.Shapes)
        {
            var result = BodyFitGeometry.Evaluate(body, shape, shape.DefaultPose);
            Check(result.Fits && result.Outside.Length == 0 && result.FailedContacts.Length == 0, "each authored default fits: " + shape.Id);
            Check(shape.Rotations.SequenceEqual(Enumerable.Range(0, 6)), "body rotation uses six orientations");
            Check(!BodyFitGeometry.Evaluate(body, shape, shape.DefaultPose with { R = 50 }).Fits, "far translated garment cannot float outside body");
            Check(BodyFitGeometry.Cells(shape.Rows).Length == result.Cells.Length, "fitting preserves worn occupancy size");
        }
        Check(new BodyHex(1, 0).Rotate(1) == new BodyHex(0, 1), "independent axial 60-degree reference vector");
        Check(new BodyHex(1, -1).Rotate(2) == new BodyHex(0, 1), "independent axial 120-degree reference vector");
        for (var rotation = 0; rotation < 6; rotation++)
        for (var direction = 0; direction < 6; direction++)
            Check(new BodyHex(0, 0).Neighbor(direction).Rotate(rotation) == new BodyHex(0, 0).Neighbor((direction + rotation) % 6), "contact directions rotate with their shape");
        try { new BodyHex(1, 0).Rotate(int.MaxValue); throw new Exception("Unbounded rotation accepted"); }
        catch (ArgumentOutOfRangeException) { Check(true, "public body rotation is bounded before iteration"); }
        var square = new BodyMask("test", "Reference body", ["#####", "#####", "#####"]);
        var cap = new BodyFitShape("cap", "test", ["..#..", ".....", "....."], new(0, 0), [0, 1, 2, 3, 4, 5], [new(new(0, 0), 5)]);
        Check(BodyFitGeometry.Evaluate(square, cap, cap.DefaultPose).Fits, "closed top edge meets exterior body boundary");
        var misplaced = BodyFitGeometry.Evaluate(square, cap, new(0, 1));
        Check(!misplaced.Fits && misplaced.Outside.Length == 0 && misplaced.FailedContacts.Length == 1,
            "contact detects bad placement even when every garment cell lies inside the body");
        Check(BodyFitGeometry.Evaluate(square, cap with { Contacts = [] }, new(0, 1)).Fits, "authored open edge permits body continuation");
        var turned = BodyFitGeometry.Evaluate(square, cap, new(0, 0, 1));
        Check(turned.Contacts.Single().Direction == 0, "closed edge direction turns with the geometry");
        Check(!BodyFitGeometry.Evaluate(square, cap, new(0, 0, 6)).Fits && !BodyFitGeometry.Evaluate(square, cap, new(0, 0, -1)).Fits, "unbounded orientations are refused without a transform loop");
        Check(!BodyFitGeometry.Evaluate(square, cap with { Rotations = [0] }, new(0, 0, 1)).Fits, "authored orientation restrictions apply");
        Check(!BodyFitGeometry.Evaluate(square with { Id = "other" }, cap, new(0, 0)).Fits, "same silhouette does not override a different body family");
        var shirt = catalog.Shape("shirt");
        Check(!BodyFitGeometry.Evaluate(body, shirt, shirt.DefaultPose with { R = shirt.DefaultPose.R + 1 }).Fits, "shirt cannot be shifted below its shoulders");
        Check(!BodyFitGeometry.Evaluate(body, shirt, shirt.DefaultPose with { Rotation = 3 }).Fits, "upside-down shirt fails its actual silhouette/contacts");
        var before = sim.Save(); var blockedShirt = sim.PreviewBodyFit("avatar-shirt", shirt.DefaultPose)!;
        Check(blockedShirt.Geometry.Fits && !blockedShirt.Handling.Allowed && sim.Save() == before, "geometric fit never bypasses current wearing state or outer-layer constraints");
        Check(sim.PreviewBodyFit("observer-coat", new(0, 0)) is null, "preview cannot inspect another employee's hidden equipment");
        Check(sim.PreviewBodyFit("chemical-suit", new(0, 0)) is null, "distant fixture's gear is not granted through the fit menu");
        Check(!sim.QuoteInventory(new(InventoryActionKind.Move, "flashlight", new("avatar-trousers", "left", 0, 2, BodyPose: new(0, 0)))).Allowed,
            "storage cannot retain a spurious worn coordinate");
        Check(sim.HandleInventory(new(InventoryActionKind.Move, "avatar-shoes", new("person:avatar", "hand"))), "remove shoes for fitting fixture"); Finish(sim);
        var bootShape = catalog.Shape("boots");
        var fit = sim.PreviewBodyFit("avatar-shoes", bootShape.DefaultPose)!;
        Check(fit.Geometry.Fits && fit.Handling.Allowed, "held shoes can be fitted through normal equipment command");
        var invalid = sim.PreviewBodyFit("avatar-shoes", bootShape.DefaultPose with { R = 0 })!;
        before = sim.Save();
        Check(!invalid.Geometry.Fits && !invalid.Handling.Allowed && !sim.HandleInventory(invalid.Request) && sim.Save() == before, "failed body fit has no physical effect or cost");
        var mixed = fit.Request with { Destination = fit.Request.Destination! with { Rotated = true } };
        Check(!sim.QuoteInventory(mixed).Allowed, "body rotation and packed orientation cannot be mixed");
        sim.ManualPause = true; before = sim.Save();
        Check(!sim.HandleInventory(fit.Request) && sim.Save() == before, "preview/apply cannot grant free dressing during pause");
        sim.ManualPause = false;
        Check(sim.HandleInventory(fit.Request) && sim.Inventory.Items["avatar-shoes"].Placement.RegionId == "hand", "fitting purchases time before committing a new body pose");
        sim.Tick(); var mid = sim.Save(); var clone = Sim(); clone.Restore(mid);
        Check(clone.Save() == mid, "pending body coordinates/costs restore exactly");
        Finish(sim); Finish(clone);
        Check(sim.Save() == clone.Save() && sim.Inventory.Items["avatar-shoes"].Placement.BodyPose == bootShape.DefaultPose, "applied worn pose persists and resumes identically");
        before = sim.Save();
        Check(!sim.HandleInventory(new(InventoryActionKind.Move, "avatar-shoes", new("person:avatar", "footwear"))) && sim.Save() == before,
            "implicit and explicit authored default are equivalent, avoiding a meaningless refit");
        Check(sim.HandleInventory(new(InventoryActionKind.Move, "avatar-shoes", new("person:avatar", "hand"))), "take fitted equipment off"); Finish(sim);
        Check(sim.Inventory.Items["avatar-shoes"].Placement.BodyPose is null, "removal clears worn position rather than carrying it into storage");
        var packing = InventoryPlacement.Footprint(sim.Inventory.Definition("avatar-shoes"), sim.Inventory.Region("avatar-backpack", "main"), false);
        Check(packing == (7, 10) && InventoryPlacement.Footprint(sim.Inventory.Definition("avatar-shoes"), sim.Inventory.Region("avatar-backpack", "main"), true) == (10, 7),
            "worn shape never alters the separate two-orientation millimetre packing envelope");
        var packSim = Sim(); packSim.Player.Cell = new(13, 6);
        Check(packSim.HandleInventory(new(InventoryActionKind.Move, "spare-backpack", new("person:avatar", "front"))), "fit second pack to front"); Finish(packSim);
        Check(packSim.PreviewBodyFit("chemical-suit", new(0, 0)) is null, "even a nearby accessible crate is not exposed as a quick-scan fit list");
        var packItem = packSim.Inventory.Items["spare-backpack"];
        Check(packSim.PreviewBodyFit(packItem.Id, catalog.Shape("backpack").DefaultPose)!.Request.Destination!.RegionId == "front", "preview preserves actual front-pack mount");
        var tactical = Sim();
        Check(tactical.HandleInventory(new(InventoryActionKind.Move, "avatar-shoes", new("person:avatar", "hand"))), "prepare held tactical equipment"); Finish(tactical);
        tactical.Player.Facing = 1; Check(tactical.ArmTraining(), "enter tactical fit fixture");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        tactical.Combat.Roster[tactical.Combat.Cursor] = tactical.Combat.Roster[tactical.Combat.Cursor] with { Ap = 10 };
        var tacticalFit = tactical.PreviewBodyFit("avatar-shoes", bootShape.DefaultPose)!;
        Check(tactical.HandleInventory(tacticalFit.Request) && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - tacticalFit.Handling.Ap, "geometric fitting uses existing equipment AP costs");
        Finish(tactical); var local = tactical.LocalMs; tactical.Tick();
        Check(tactical.LocalMs == local, "tactical input hold resumes after fitting");
        void Reject(Action<JsonNode> mutate)
        {
            var victim = Sim(); var safe = victim.Save(); var bad = JsonNode.Parse(mid)!; mutate(bad);
            try { victim.Restore(bad.ToJsonString()); throw new Exception("Invalid body continuation accepted"); }
            catch (ArgumentException) { Check(victim.Save() == safe, "invalid worn pose restore preserves the running simulation"); }
        }
        Reject(n => n["Inventory"]!["Pending"]!["Request"]!["Destination"]!["BodyPose"]!["Rotation"] = 99);
        Reject(n => n["Inventory"]!["Pending"]!["Request"]!["Destination"]!["BodyPose"]!["Q"] = 9999);
        Reject(n => n["Inventory"]!["Items"]!.AsArray().First(i => i!["Id"]!.GetValue<string>() == "avatar-coat")!["Placement"]!["BodyPose"] = new JsonObject { ["Q"] = 0, ["R"] = 0, ["Rotation"] = 0 });
        var authored = catalog.Content;
        void RejectCatalog(BodyFitContent bad)
        {
            try { BodyFitCatalog.Validate(bad); throw new Exception("Invalid authored body content accepted"); }
            catch (ArgumentException) { passed++; }
        }
        RejectCatalog(authored with { Bodies = [body with { Rows = ["..X.."] }] });
        RejectCatalog(authored with { Shapes = [shirt with { Contacts = [new(new(999, 999), 0)] }] });
        RejectCatalog(authored with { Shapes = [shirt with { Contacts = [new(new(0, 0), 9)] }] });
        RejectCatalog(authored with { Shapes = [shirt with { Rotations = [0, 0] }] });
        RejectCatalog(authored with { Shapes = [shirt with { BodyFamily = "absent" }] });
        RejectCatalog(authored with { Shapes = [shirt with { DefaultPose = new(0, 0, 1) }] });
        Console.WriteLine($"BODY_FIT_CHECKS_PASS: {passed} assertions; mask/contact geometry, six orientations, paid equipment, isolation and persistence.");
        return passed;
    }
}
