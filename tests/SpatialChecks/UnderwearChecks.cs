using SiteSimulation;
using System.Text.Json.Nodes;

/// <summary>The new base garment is real equipment: coverage, paid removal and continuation use ordinary inventory rules.</summary>
internal static class UnderwearChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Underwear: " + reason); passed++; }
        SpatialSimulation Sim() { var s = new SpatialSimulation(map, config); s.Player.Cell = new(13, 6); return s; }
        void Finish(SpatialSimulation s)
        { for (var i = 0; i < 1000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "paid handling resolves"); }
        void Act(SpatialSimulation s, InventoryRequest request) { Check(s.HandleInventory(request), s.LastMessage); Finish(s); }
        var sim = Sim(); var inv = sim.Inventory; var id = "avatar-underwear"; var waist = new BodyHex(-6, 12);
        var definition = inv.Definition(id); var underwear = inv.Items[id];
        Check(definition.Id == "underwear" && definition.MassGrams == 80 && definition.Regions is null, "base garment has physical identity/mass without invented storage");
        Check(underwear.Placement is { ParentId: "person:avatar", RegionId: "underwear" }
            && InventoryWornOrder.Order(underwear) < InventoryWornOrder.Order(inv.Items["avatar-trousers"]), "briefs are worn before trousers in the real order");
        var shape = inv.BodyFits!.Shape("underwear"); var fit = BodyFitGeometry.Evaluate(inv.BodyFits.Body(shape.BodyFamily), shape, shape.DefaultPose);
        Check(fit.Fits && fit.Cells.All(c => c.R is >= 11 and <= 13), "authored briefs fit the pelvis/upper thigh rather than the whole trouser leg");
        Check(InventoryWornStack.Cells(inv, underwear).Select(c => c.Surface).Distinct().Order().SequenceEqual(new[] { "back", "front" }), "one garment wraps both sides");
        Check(InventoryWornOrder.Layers(inv, id).All(l => l.Layer == 0)
            && sim.InventoryWornCells.Single(c => c.ItemId == "avatar-trousers" && c.Cell == new BodySurfaceCell("front", waist)).Layer == 1,
            "briefs add the physical base layer beneath trousers");
        Check(!InventoryWear.Visible(inv, underwear) && !sim.InventoryMoveAccess(id).Allowed, "covered briefs cannot be seen or removed through trousers");
        var before = sim.Save(); var remove = new InventoryRequest(InventoryActionKind.Move, id, new("person:avatar", "hand"));
        Check(!sim.HandleInventory(remove) && sim.Save() == before, "covered removal rejects atomically");
        // A valid prepared staging fixture: helpers hold the removed outer gear.
        // This is test setup, not a new player handoff feature or enlarged game crate.
        inv.Items["avatar-backpack"] = inv.Items["avatar-backpack"] with { Placement = new("person:observer", "hand") };
        inv.Items["avatar-coat"] = inv.Items["avatar-coat"] with { Placement = new("person:tech", "hand") };
        InventoryPlacement.Validate(inv);
        Act(sim, new(InventoryActionKind.Move, "spare-backpack", new("person:avatar", "front")));
        var phone = inv.Items["field-phone"].Placement;
        Act(sim, new(InventoryActionKind.AutoPlace, "avatar-trousers", new("fixture:crate-a", "store")));
        Check(inv.Items["field-phone"].Placement == phone, "loaded trousers retain their real pocket descendants");
        Check(sim.InventoryBodyPick("front", waist)?.ItemId == id && sim.InventoryBodyPick("back", waist)?.ItemId == id
            && sim.InventoryMoveAccess(id).Allowed, "clearing the real outer layers exposes the same briefs on both sides");
        sim.ManualPause = true; before = sim.Save(); Check(!sim.HandleInventory(remove) && sim.Save() == before, "pause supplies no free removal");
        sim.ManualPause = false; var mass = InventoryHierarchy.CarriedMass(inv, "person:avatar");
        Check(sim.HandleInventory(remove) && inv.Items[id].Placement == underwear.Placement && inv.Pending is { TotalMs: > 0, CostAp: > 0 },
            "paid work retains the source and ordinary costs until commitment");
        var pending = sim.Save(); var clone = Sim(); clone.Restore(pending); Check(clone.Save() == pending, "new loadout pending work restores exactly");
        Finish(sim); Finish(clone);
        Check(sim.Save() == clone.Save(), "restored removal resolves identically");
        Check(inv.Items[id].Placement is { RegionId: "hand", WornOrder: null, BodyPose: null }
            && sim.InventoryBodyPick("front", waist) is null && InventoryHierarchy.CarriedMass(inv, "person:avatar") == mass,
            "completion clears worn state and counts the same garment mass once in hand");
        var corrupt = JsonNode.Parse(sim.Save())!;
        var items = corrupt["Inventory"]!["Items"]!.AsArray(); items.Remove(items.Single(n => n!["Id"]!.GetValue<string>() == id));
        before = sim.Save();
        try { sim.Restore(corrupt.ToJsonString()); throw new Exception("Missing underwear was accepted"); }
        catch (ArgumentException) { Check(sim.Save() == before, "new content still requires its exact item set on atomic restore"); }
        Console.WriteLine($"UNDERWEAR_CHECKS_PASS: {passed} assertions; real base layer, coverage, loaded clothing, paid removal, mass and continuation.");
        return passed;
    }
}
