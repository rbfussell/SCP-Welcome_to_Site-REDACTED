using SiteSimulation;

internal static class WearFitChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Wear fit: " + detail); passed++; }
        var content = new SpatialSimulation(map, config).Inventory;
        var shirt = content.Content!.Definitions.Single(d => d.Id == "shirt");
        var jacket = content.Content.Definitions.Single(d => d.Id == "coat");
        var pack = content.Content.Definitions.Single(d => d.Id == "backpack");
        var parka = jacket with { Id = "test-parka", Name = "Bulky parka", Wear = jacket.Wear! with
            { Fit = jacket.Wear.Fit! with { Family = "core:bulky-coat", Bulk = 8 } } };
        var armor = shirt with { Id = "test-armor", Name = "Flexible body armor", Wear = shirt.Wear! with
            { Fit = new("core:flexible-armor", 2, 6, ["core:light-textile", "core:light-jacket"]) } };
        Check(InventoryWearFit.Evaluate(shirt, [jacket]).Fits && InventoryWearFit.Evaluate(jacket, [shirt]).Fits,
            "a roomy shirt and light jacket admit either proposed order without consulting their authored layers");
        Check(!InventoryWearFit.Evaluate(shirt, [parka]).Fits, "a shirt cannot fit over a bulky parka");
        Check(InventoryWearFit.Evaluate(armor, [jacket]).Fits && InventoryWearFit.Evaluate(jacket, [armor]).Fits,
            "flexible armor and light jacket can fit in either order");
        var exact = InventoryWearFit.Evaluate(shirt, [jacket, armor]);
        Check(exact is { Fits: true, UnderBulk: 5, Capacity: 5 }, "combined underlying bulk can exactly consume clearance");
        var over = InventoryWearFit.Evaluate(shirt, [jacket, armor, shirt]);
        Check(over is { Fits: false, UnderBulk: 6, Capacity: 5 } && over.Reason.Contains("clearance"),
            "individually compatible garments still fail when their combined bulk exceeds capacity");
        Check(InventoryWearFit.Evaluate(shirt, [shirt, shirt, shirt, shirt, shirt]).Fits
            && !InventoryWearFit.Evaluate(shirt, [shirt, shirt, shirt, shirt, shirt, shirt]).Fits,
            "repeated definitions are distinct garments and each counts, rather than being deduplicated");
        Check(InventoryWearFit.Evaluate(shirt, [armor, jacket]) == exact, "bulk evaluation is stable under input enumeration order");
        Check(InventoryWearFit.Evaluate(shirt with { Wear = shirt.Wear! with { Layer = 9 } }, [parka]).Fits == false
            && InventoryWearFit.Evaluate(shirt with { Wear = shirt.Wear! with { Layer = 0 } }, [jacket]).Fits,
            "changing legacy layer numbers cannot manufacture or remove compatibility");
        Check(InventoryWearFit.Evaluate(shirt with { MassGrams = 100000 }, [jacket]) == InventoryWearFit.Evaluate(shirt, [jacket]),
            "fit bulk does not reinterpret mass or storage size");
        var shell = jacket with { Tags = ["core:rigid-shell"] };
        Check(!InventoryWearFit.Evaluate(shirt, [shell]).Fits && InventoryWearFit.Evaluate(jacket, [shirt]).Fits,
            "explicit exclusions override an otherwise accepted family and are directional");
        Check(!InventoryWearFit.Evaluate(pack, [pack]).Fits && InventoryWearFit.Evaluate(pack, [pack]).Reason.Contains("outermost"),
            "an extra fit budget cannot authorize covering an outermost pack");
        var unknown = shirt with { Wear = shirt.Wear! with { Fit = shirt.Wear.Fit! with { Family = "mod:unknown-family" } } };
        Check(!InventoryWearFit.Evaluate(jacket, [unknown]).Fits, "unlisted families fail closed");
        Check(!InventoryWearFit.Evaluate(shirt, [jacket with { Wear = jacket.Wear! with { Fit = null } }]).Fits,
            "missing underlying fit metadata is not treated as zero bulk");
        Check(!InventoryWearFit.Evaluate(shirt, [jacket with { Tags = null! }]).Fits,
            "missing underlying trait data fails safely instead of bypassing exclusions");
        var fit = shirt.Wear!.Fit!;
        foreach (var invalid in new[] { fit with { Family = "" }, fit with { Family = null! }, fit with { Bulk = 0 },
            fit with { Bulk = 1001 }, fit with { MaxUnderBulk = -1 }, fit with { MaxUnderBulk = 1001 },
            fit with { FitsOver = null! }, fit with { FitsOver = ["core:light-textile", "core:light-textile"] },
            fit with { FitsOver = [null!] }, fit with { ExcludesUnderTags = ["bad token"] } })
        {
            Check(!InventoryWearFit.ProfileValid(invalid), "malformed fit profile rejected");
            Check(!InventoryWearFit.Evaluate(shirt with { Wear = shirt.Wear with { Fit = invalid } }, []).Fits,
                "pure fit query cannot bypass authored metadata validation");
        }
        Check(InventoryWearFit.Evaluate(shirt with { Wear = shirt.Wear with { Fit = fit with { MaxUnderBulk = 0, FitsOver = [] } } }, []).Fits,
            "zero under-clearance still allows a garment against bare skin");

        // The bridge exercises the actual inventory graph; no UI or item-definition special cases choose fit.
        var world = new SimulationWorld(map, config); var state = world.Inventory;
        void Change(string id, Func<ItemDefinition, ItemDefinition> change)
        { var index = Array.FindIndex(state.Content!.Definitions, d => d.Id == id); state.Content.Definitions[index] = change(state.Content.Definitions[index]); }
        state.Items["avatar-backpack"] = state.Items["avatar-backpack"] with { Placement = new("fixture:crate-a", "store", 0, 8) };
        state.Items["avatar-shirt"] = state.Items["avatar-shirt"] with { Placement = state.Items["avatar-shirt"].Placement with { WornOrder = 7 } };
        InventoryPlacement.Validate(state);
        Check(true, "graph bridge accepts a compatible shirt above the light jacket in a proposed ordering");
        Change("coat", d => d with { Name = parka.Name, Wear = parka.Wear });
        try { InventoryPlacement.Validate(state); throw new Exception("Incompatible graph accepted"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("parka"), "graph validation rejects the incompatible reversed combination with a named reason"); }

        world = new SimulationWorld(map, config); state = world.Inventory;
        Change("backpack", d => d with { Wear = d.Wear! with { Fit = d.Wear.Fit! with { MaxUnderBulk = 4 } } });
        Change("boots", d => d with { Wear = d.Wear! with { Fit = d.Wear.Fit! with { Bulk = 1000 } } });
        InventoryPlacement.Validate(state);
        Check(true, "per-region graph check counts jacket plus shirt once, and ignores bulky footwear on a separate region");
        Change("backpack", d => d with { Wear = d.Wear! with { Fit = d.Wear.Fit! with { MaxUnderBulk = 3 } } });
        try { InventorySaveCodec.ValidateGraph(state); throw new Exception("Overfilled fit graph accepted"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("clearance"), "save-graph boundary also enforces cumulative fit even with the jacket open"); }

        world = new SimulationWorld(map, config); state = world.Inventory;
        Change("shirt", d => d with { Wear = d.Wear! with { Fit = null } });
        try { InventoryContentValidation.Validate(world); throw new Exception("Missing fit content accepted"); }
        catch (ArgumentException) { Check(true, "native wearable content requires fit metadata independently of current worn membership"); }

        SpatialSimulation Sim() => new(map, config);
        InventoryRequest Shoes(string region) => new(InventoryActionKind.Move, "avatar-shoes", new("person:avatar", region));
        void Finish(SpatialSimulation s)
        { for (var i = 0; i < 1000 && s.Inventory.Pending is not null; i++) s.Tick(); Check(s.Inventory.Pending is null, "paid handling resolves"); }
        var sim = Sim();
        Check(sim.HandleInventory(Shoes("hand")), sim.LastMessage); Finish(sim);
        var held = sim.Inventory.Items["avatar-shoes"].Placement;
        var index = Array.FindIndex(sim.Inventory.Content!.Definitions, d => d.Id == "boots");
        var boots = sim.Inventory.Content.Definitions[index];
        sim.Inventory.Content.Definitions[index] = boots with { Wear = boots.Wear! with { Fit = boots.Wear.Fit! with { MaxUnderBulk = 0 } } };
        var safe = sim.Save();
        Check(!sim.QuoteInventory(Shoes("footwear")).Allowed && !sim.HandleInventory(Shoes("footwear")) && sim.Save() == safe,
            "ordinary move quotes and starts reject insufficient fit atomically");
        sim.Inventory.Content.Definitions[index] = boots;
        var quote = sim.QuoteInventory(Shoes("footwear"));
        Check(quote is { Allowed: true, DurationMs: > 0, Ap: > 0 }, "compatible fit retains normal handling cost");
        sim.ManualPause = true; safe = sim.Save();
        Check(!sim.HandleInventory(Shoes("footwear")) && sim.Save() == safe, "compatibility grants no free handling while paused");
        sim.ManualPause = false;
        Check(sim.HandleInventory(Shoes("footwear")) && sim.Inventory.Items["avatar-shoes"].Placement == held,
            "compatible equipment stays at its source while handling runs");
        sim.Tick(); var middle = sim.Save(); var clone = Sim(); clone.Restore(middle);
        Check(clone.Save() == middle, "fit-qualified pending work restores exactly with immutable profiles");
        Finish(sim); Finish(clone);
        Check(sim.Save() == clone.Save() && sim.Inventory.Items["avatar-shoes"].Placement.RegionId == "footwear",
            "fit-qualified handling resumes identically and commits once");

        // Revalidation must apply the new fit contract as well as the old access/placement checks.
        sim = Sim(); Check(sim.HandleInventory(Shoes("hand")), sim.LastMessage); Finish(sim);
        Check(sim.HandleInventory(Shoes("footwear")), sim.LastMessage);
        sim.Inventory.Content!.Definitions[index] = boots with { Wear = boots.Wear! with { Fit = boots.Wear.Fit! with { MaxUnderBulk = 0 } } };
        Finish(sim);
        Check(sim.Inventory.Items["avatar-shoes"].Placement.RegionId == "hand", "a destination that no longer fits cannot commit stale work");

        var tactical = Sim(); Check(tactical.HandleInventory(Shoes("hand")), tactical.LastMessage); Finish(tactical);
        tactical.Player.Facing = 1; Check(tactical.ArmTraining() && tactical.Combat.Active, "fit/AP fixture starts real tactical combat");
        tactical.Combat.Cursor = tactical.Combat.Roster.FindIndex(c => c.ActorId == tactical.Player.Id);
        var turn = tactical.Combat.Cursor;
        quote = tactical.QuoteInventory(Shoes("footwear"));
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = quote.Ap - 1 }; safe = tactical.Save();
        Check(!tactical.HandleInventory(Shoes("footwear")) && tactical.Save() == safe, "a valid fit still needs sufficient combat AP");
        tactical.Combat.Roster[turn] = tactical.Combat.Roster[turn] with { Ap = 10 };
        tactical.Inventory.Content!.Definitions[index] = boots with { Wear = boots.Wear! with { Fit = boots.Wear.Fit! with { MaxUnderBulk = 0 } } };
        safe = tactical.Save();
        Check(!tactical.HandleInventory(Shoes("footwear")) && tactical.Save() == safe, "an invalid fit spends no AP or tactical time");
        tactical.Inventory.Content.Definitions[index] = boots;
        Check(tactical.HandleInventory(Shoes("footwear")) && tactical.Combat.For(tactical.Player.Id)!.Ap == 10 - quote.Ap,
            "fit-qualified work consumes quoted combat AP exactly once");
        Finish(tactical);
        Check(tactical.Inventory.Items["avatar-shoes"].Placement.RegionId == "footwear", "fit-qualified combat work commits through normal resolution");
        Console.WriteLine($"WEAR_FIT_CHECKS_PASS: {passed} assertions; directional compatibility, cumulative bulk, exclusions, region isolation, validated work and continuation.");
        return passed;
    }
}
