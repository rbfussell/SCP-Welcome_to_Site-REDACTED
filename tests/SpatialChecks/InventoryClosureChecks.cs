using SiteSimulation;
using System.Text.Json.Nodes;

internal static class InventoryClosureChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string reason) { if (!ok) throw new Exception("Closure policy: " + reason); passed++; }
        SpatialSimulation Sim() => new(map, config);
        InventoryRequest Pocket(string id, string region) => new(InventoryActionKind.Close, id, RegionId: region);
        void Finish(SpatialSimulation sim)
        {
            for (var i = 0; i < 400 && (sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining > 0); i++) sim.Tick();
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "work finishes under normal scheduler");
        }
        var sim = Sim(); var state = sim.Inventory;
        var jacket = new InventoryRequest(InventoryActionKind.CloseGarment, "avatar-coat");
        var chest = Pocket("avatar-coat", "chest"); var pack = Pocket("avatar-backpack", "main");
        Check(!InventoryClosures.SecureOnClose(state, "avatar-coat") && InventoryClosures.SecureOnClose(state, "avatar-coat", "chest"),
            "jacket front stays open by default while its independent buttoned pocket auto-secures");
        Check(InventoryClosures.SecureOnClose(state, "avatar-backpack", "main"), "backpack main closure keeps its own authored default");
        Check(InventoryClosures.OnDismissal(state, [jacket, chest, pack]).SequenceEqual(new[] { chest, pack }), "dismissal filters each fastening independently");
        var unchanged = sim.Save();
        Check(sim.SecureInventory([jacket], automatic: true) && sim.Save() == unchanged && state.SecuringRemaining == 0,
            "automatic service callers also honor defaults without requiring the UI to filter targets");
        Check(InventoryClosures.Toggle(state, "avatar-coat").Verb == "Zip", "open jacket offers Zip");
        sim.HandleInventory(jacket); Finish(sim);
        var unzip = InventoryClosures.Toggle(state, "avatar-coat");
        Check(unzip.Verb == "Unzip" && unzip.Request.Kind == InventoryActionKind.OpenGarment, "secured jacket offers Unzip from its actual boolean state");
        sim.HandleInventory(unzip.Request); Finish(sim);
        Check(InventoryClosures.Toggle(state, "avatar-coat").Verb == "Zip", "opening returns the opposite physical action");
        var beforeRevision = state.Revision;
        sim.ManualPause = true;
        Check(sim.SetInventorySecureOnClose("avatar-coat", null, true), "player may opt this jacket front into auto-securing even while paused");
        Check(InventoryClosures.OnDismissal(state, [jacket]).Length == 1 && state.Items["avatar-coat"].GarmentOpen && state.Revision == beforeRevision && state.Pending is null,
            "preference changes do not zip the jacket, buy AP/time or invalidate paid work");
        Check(!InventoryClosures.SecureOnClose(state, "observer-coat"), "another jacket instance retains its independent authored default");
        Check(sim.SetInventorySecureOnClose("avatar-coat", "chest", false) && InventoryClosures.SecureOnClose(state, "avatar-coat")
            && !InventoryClosures.SecureOnClose(state, "avatar-coat", "chest"), "pocket preference is independent of the garment front");
        Check(sim.SetInventorySecureOnClose("avatar-backpack", "main", false) && InventoryClosures.SecureOnClose(state, "avatar-backpack", "side"),
            "one compartment preference does not affect another compartment of the same bag");
        var saved = sim.Save(); var clone = Sim(); clone.Restore(saved);
        Check(clone.Save() == saved && InventoryClosures.SecureOnClose(clone.Inventory, "avatar-coat") && !InventoryClosures.SecureOnClose(clone.Inventory, "avatar-coat", "chest"),
            "instance and compartment overrides persist exactly");
        var before = sim.Save();
        Check(!sim.SetInventorySecureOnClose("tech-coat", null, true) && sim.Save() == before, "cannot configure another employee's clothing");
        Check(!sim.SetInventorySecureOnClose("avatar-coat", "unknown", false) && sim.Save() == before, "invalid compartment rejects without changing any state");
        Check(!sim.SetInventorySecureOnClose("flashlight", null, false) && sim.Save() == before, "an ordinary item has no invented fastening");
        sim.ManualPause = false;
        Check(sim.SecureInventory([pack]), "explicit securing still works with automatic behavior disabled"); Finish(sim);
        Check(!state.IsOpen("avatar-backpack", "main") && InventoryClosures.Toggle(state, "avatar-backpack", "main").Verb == "Unzip",
            "explicit closure commits and exposes the opposite menu verb");
        var unbutton = InventoryClosures.Toggle(state, "avatar-coat", "chest");
        Check(unbutton.Verb == "Button", "open pocket offers Button");
        sim.HandleInventory(unbutton.Request); Finish(sim);
        Check(InventoryClosures.Toggle(state, "avatar-coat", "chest").Verb == "Unbutton", "secured pocket offers Unbutton independently of its auto-close preference");
        var canceled = Sim(); canceled.SecureInventory([chest, pack], automatic: true);
        Check(canceled.SetInventorySecureOnClose("avatar-coat", "chest", false) && canceled.Inventory.SecuringRemaining == 1,
            "turning off one fastening removes only its unstarted automatic closure");
        var explicitPlan = Sim(); explicitPlan.SecureInventory([chest]);
        Check(explicitPlan.SetInventorySecureOnClose("avatar-coat", "chest", false) && explicitPlan.Inventory.SecuringRemaining == 1,
            "per-gear convenience changes do not cancel an explicitly ordered closure");
        var paid = Sim(); paid.SecureInventory([pack]); paid.Tick();
        Check(paid.Inventory.Pending is not null && paid.SetInventorySecureOnClose("avatar-backpack", "main", false), "preference can change while paid work resolves");
        Finish(paid);
        Check(!paid.Inventory.IsOpen("avatar-backpack", "main"), "already committed work still completes without a free refund or revision failure");
        // Existing coarse body regions already distinguish two independent access locations. Hex masks follow later.
        var world = new SimulationWorld(map, config); var definitions = world.Inventory.Content!.Definitions;
        var index = Array.FindIndex(definitions, d => d.Id == "trousers"); var trousers = definitions[index];
        definitions[index] = trousers with { Regions = trousers.Regions!.Select(r => r with { AccessBody = r.Id == "left" ? ["hips-front"] : ["legs"] }).ToArray() };
        world.Inventory.Items["avatar-coat"] = world.Inventory.Items["avatar-coat"] with { GarmentOpen = false };
        Check(!InventoryAccessService.Placement(world, "avatar", new("avatar-trousers", "left")).Allowed
            && InventoryAccessService.Placement(world, "avatar", new("avatar-trousers", "right")).Allowed,
            "jacket over hips covers the hip pocket but leaves the independently located leg pocket accessible");
        var corrupt = JsonNode.Parse(saved)!;
        corrupt["Inventory"]!["Items"]!.AsArray().Single(n => n!["Id"]!.GetValue<string>() == "flashlight")!["SecureOnClose"] = false;
        var safe = clone.Save();
        try { clone.Restore(corrupt.ToJsonString()); throw new Exception("Unsupported item closure override accepted"); }
        catch (ArgumentException) { Check(clone.Save() == safe, "unsupported closure metadata rejects atomically on restore"); }
        var legacy = JsonNode.Parse(new SpatialSimulation(map, config).Save())!; legacy["Version"] = 9;
        foreach (var item in legacy["Inventory"]!["Items"]!.AsArray()) item!.AsObject().Remove("SecureOnClose");
        foreach (var region in legacy["Inventory"]!["Regions"]!.AsArray()) region!.AsObject().Remove("SecureOnClose");
        clone.Restore(legacy.ToJsonString());
        Check(!InventoryClosures.SecureOnClose(clone.Inventory, "avatar-coat") && InventoryClosures.SecureOnClose(clone.Inventory, "avatar-coat", "chest"),
            "matching older snapshots inherit the authored defaults without inventing player overrides");
        Console.WriteLine($"INVENTORY_CLOSURE_CHECKS_PASS: {passed} assertions; state verbs, per-fastening defaults/overrides, pocket coverage, paid work and persistence.");
        return passed;
    }
}
