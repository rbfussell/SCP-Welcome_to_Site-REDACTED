using SiteSimulation;
using System.Text.Json.Nodes;

internal static class WearChecks
{
    public static int Run(string map, string config)
    {
        var passed = 0;
        void Check(bool ok, string detail) { if (!ok) throw new Exception("Wear: " + detail); passed++; }
        SpatialSimulation Sim() => new(map, config);
        InventoryItem Item(SpatialSimulation s, string id) => s.Inventory.DiagnosticItems.Single(i => i.Id == id);
        InventoryRequest Move(string id, string parent, string region, int x = 0, int y = 0)
            => new(InventoryActionKind.Move, id, new(parent, region, x, y));
        void Resolve(SpatialSimulation s)
        {
            for (var i = 0; s.Inventory.Pending is not null && i < 1000; i++) s.Tick();
            Check(s.Inventory.Pending is null, "timed work failed to resolve");
        }
        void Act(SpatialSimulation s, InventoryRequest request)
        { Check(s.HandleInventory(request), s.LastMessage); Resolve(s); }

        var sim = Sim();
        Check(Item(sim, "avatar-coat").GarmentOpen && sim.InventoryContents("person:avatar", "torso").Count == 1,
            "an open coat exposes the shirt on the paperdoll");
        Check(sim.InventoryContents("person:avatar", "socks").Count == 0 && !sim.InventoryInspectAccess("avatar-socks").Allowed,
            "shoes conceal the socks underneath");
        var unchanged = sim.Save();
        Check(sim.InventoryPaperdollContents("socks", true, 0).Single().Id == "avatar-socks"
            && sim.InventoryPaperdollContents("socks").Count == 0 && sim.InventoryPaperdollContents("socks", true, 1).Count == 0,
            "layer inspection reveals only known equipment on the chosen layer without changing ordinary visibility");
        Check(sim.InventoryPaperdollContents("pieces", true).Count == 0 && sim.InventoryPaperdollContents("main", true).Count == 0
            && sim.InventoryPaperdollContents("socks", true, 10).Count == 0,
            "paperdoll projection cannot expose nested storage or invent layers");
        Check(!sim.InventoryMoveAccess("avatar-socks").Allowed && sim.InventoryMoveAccess("avatar-shoes").Allowed && sim.Save() == unchanged,
            "inspecting a layer cannot grant manipulation or mutate state");
        Check(!sim.HandleInventory(Move("avatar-socks", "person:avatar", "hand")) && sim.Save() == unchanged,
            "covered socks cannot be removed through shoes");
        Act(sim, Move("avatar-shoes", "person:avatar", "hand"));
        Check(sim.InventoryContents("person:avatar", "socks").Single().Id == "avatar-socks", "removing shoes exposes socks");
        Act(sim, Move("avatar-shoes", "person:avatar", "footwear"));
        Check(sim.InventoryContents("person:avatar", "socks").Count == 0, "replacing shoes conceals socks again");
        var close = new InventoryRequest(InventoryActionKind.CloseGarment, "avatar-coat");
        Check(sim.HandleInventory(close) && Item(sim, "avatar-coat").GarmentOpen, "starting fastening must not prematurely change coverage");
        sim.Tick(); var midway = sim.Save(); var clone = Sim(); clone.Restore(midway);
        Check(clone.Save() == midway, "fastening progress and clothing state restore exactly");
        for (var i = 0; i < 30; i++) { sim.Tick(); clone.Tick(); }
        Check(sim.Save() == clone.Save() && !Item(sim, "avatar-coat").GarmentOpen, "restored fastening commits once under the ordinary scheduler");
        Check(!sim.InventoryAccess("avatar-trousers", "left").Allowed && sim.InventoryContents("avatar-trousers", "left").Count == 0,
            "a fastened coat blocks and hides covered trouser pockets");
        Check(sim.InventoryPaperdollContents("torso", true, 1).Single().Id == "avatar-shirt" && !sim.InventoryMoveAccess("avatar-shirt").Allowed,
            "a covered shirt can be identified in layer view while removal remains blocked");
        Check(sim.InventoryContents("person:avatar", "legs").Count == 1 && sim.InventoryContents("person:avatar", "torso").Count == 0,
            "partial visible legs do not reveal a fully covered shirt");
        Check(!sim.OscpAccess(OscpDevice.Phone).Available && sim.InventoryAccess("avatar-backpack", "side").Allowed,
            "coverage gates the actual phone path without blocking outer pack storage");
        unchanged = sim.Save();
        Check(!sim.QuoteInventory(Move("field-phone", "person:avatar", "hand")).Allowed && sim.Save() == unchanged,
            "a known item's location cannot bypass coverage during a quote");
        Check(!sim.HandleInventory(new(InventoryActionKind.OpenGarment, "avatar-backpack"))
            && !sim.HandleInventory(new(InventoryActionKind.OpenGarment, "avatar-coat", RegionId: "chest")),
            "unsupported garment openings and mixed compartment requests are rejected");
        var open = new InventoryRequest(InventoryActionKind.OpenGarment, "avatar-coat");
        Act(sim, open);
        Check(sim.OscpAccess(OscpDevice.Phone).Available && sim.InventoryContents("person:avatar", "torso").Count == 1,
            "opening the coat exposes the front pocket path and shirt");
        Check(!sim.QuoteInventory(Move("avatar-shirt", "person:avatar", "hand")).Allowed,
            "an open front does not remove sleeves or the back when undressing a shirt");

        var suit = Sim(); suit.Player.Cell = new(13, 6);
        var equipSuit = Move("chemical-suit", "person:avatar", "suit");
        unchanged = suit.Save();
        Check(!suit.HandleInventory(equipSuit) && suit.LastMessage.Contains("backpack", StringComparison.OrdinalIgnoreCase) && suit.Save() == unchanged,
            "putting a suit beneath a worn backpack rejects with a concrete blocker and no mutation");
        Act(suit, Move("avatar-backpack", "person:avatar", "hand"));
        Act(suit, equipSuit);
        Check(suit.InventoryContents("person:avatar", "suit").Single().Id == "chemical-suit"
            && !suit.InventoryAccess("avatar-trousers", "right").Allowed && !suit.OscpAccess(OscpDevice.Phone).Available,
            "the single multi-region suit blocks its covered inner equipment");
        Check(!suit.HandleInventory(open), "a covered coat cannot be opened through a sealed suit");
        Act(suit, Move("avatar-backpack", "person:avatar", "back"));
        Check(Item(suit, "avatar-backpack").Placement.RegionId == "back", "the pack can be refitted over a compatible suit");
        Check(!suit.QuoteInventory(Move("chemical-suit", "fixture:crate-a", "store", 6, 0)).Allowed,
            "the suit cannot be removed through a refitted pack");
        Act(suit, Move("spare-backpack", "person:avatar", "front"));
        Check(suit.InventoryContents("person:avatar", "back").Count == 1 && suit.InventoryContents("person:avatar", "front").Count == 1,
            "front and back packs occupy independent mounts under the same load accounting");
        Act(suit, Move("avatar-backpack", "person:avatar", "hand"));
        Check(!suit.QuoteInventory(Move("chemical-suit", "fixture:crate-a", "store", 6, 0)).Allowed,
            "removing one pack does not clear the other pack's dressing dependency");
        var saved = suit.Save(); var restored = Sim(); restored.Restore(saved);
        Check(restored.Save() == saved, "multi-region coverage and both pack locations survive restore without duplication");
        var bad = JsonNode.Parse(saved)!;
        bad["Inventory"]!["Items"]!.AsArray().Single(n => n!["Id"]!.GetValue<string>() == "chemical-suit")!["GarmentOpen"] = true;
        try { restored.Restore(bad.ToJsonString()); throw new Exception("Unsupported suit state accepted"); }
        catch (ArgumentException) { Check(restored.Save() == saved, "invalid opening state rejects atomically"); }

        var alias = new SimulationWorld(map, config); var state = alias.Inventory;
        var socksIndex = Array.FindIndex(state.Content!.Definitions, d => d.Id == "socks");
        var socks = state.Content.Definitions[socksIndex];
        state.Content.Definitions[socksIndex] = socks with { Wear = socks.Wear! with { Mounts = [new("socks", ["feet"]), new("footwear", ["feet"])] } };
        state.Items["avatar-shoes"] = state.Items["avatar-shoes"] with { Placement = new("person:avatar", "hand") };
        state.Items["spare-socks"] = state.Items["spare-socks"] with { Placement = new("person:avatar", "footwear") };
        try { InventoryPlacement.Validate(state); throw new Exception("Duplicate body layer accepted"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("same body layer"), "different slots cannot bypass overlapping anatomical layer claims"); }
        state.Content.Definitions[socksIndex] = state.Content.Definitions[socksIndex] with
        { Wear = state.Content.Definitions[socksIndex].Wear! with { OverlapTags = ["socks"] } };
        InventoryPlacement.Validate(state);
        Check(true, "mutually compatible same-layer items can overlap anatomy across distinct mounts");
        var bootsIndex = Array.FindIndex(state.Content.Definitions, d => d.Id == "boots");
        state.Content.Definitions[bootsIndex] = state.Content.Definitions[bootsIndex] with
        { Wear = new(0, [new("footwear", ["feet"])], OverlapTags: ["socks"]) };
        state.Items["spare-socks"] = state.Items["spare-socks"] with { DefinitionId = "boots" };
        try { InventoryPlacement.Validate(state); throw new Exception("One-sided overlap accepted"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("same body layer"), "overlap permission must be compatible in both profiles"); }

        // Isolate access-covering from removal-blocking, including the confirmed non-covering, removal-blocking belt combination.
        foreach (var covers in new[] { false, true })
        foreach (var blocks in new[] { false, true })
        {
            var rules = new SimulationWorld(map, config); var inventory = rules.Inventory;
            var coatIndex = Array.FindIndex(inventory.Content!.Definitions, d => d.Id == "coat");
            var coat = inventory.Content.Definitions[coatIndex];
            inventory.Content.Definitions[coatIndex] = coat with { Wear = coat.Wear! with
            { CoversAccess = covers, BlocksRemoval = blocks, OpenExposes = coat.Wear.Mounts[0].Coverage } };
            inventory.Items["avatar-coat"] = inventory.Items["avatar-coat"] with { GarmentOpen = false };
            inventory.Items["avatar-backpack"] = inventory.Items["avatar-backpack"] with { Placement = new("fixture:crate-a", "store", 0, 8) };
            InventoryContentValidation.Validate(rules);
            Check(InventoryAccessService.Placement(rules, "avatar", new("avatar-trousers", "left")).Allowed == !covers,
                $"pocket access independently follows CoversAccess={covers}, BlocksRemoval={blocks}");
            Check(InventoryActions.Validate(rules, "avatar", Move("avatar-shirt", "person:avatar", "hand"), out _, out _, out _) == !(covers || blocks),
                "closed coverage requires opening for access; removal blocking independently requires removal");
            inventory.Items["avatar-coat"] = inventory.Items["avatar-coat"] with { GarmentOpen = true };
            Check(InventoryAccessService.Placement(rules, "avatar", new("avatar-trousers", "left")).Allowed,
                "opening an authored covering exposes its pocket access path");
            Check(InventoryActions.Validate(rules, "avatar", Move("avatar-shirt", "person:avatar", "hand"), out _, out _, out _) == !blocks,
                "opening an item does not remove its explicit removal block");
        }

        var invalidMetadata = new SimulationWorld(map, config);
        var ci = Array.FindIndex(invalidMetadata.Inventory.Content!.Definitions, d => d.Id == "coat");
        var cd = invalidMetadata.Inventory.Content.Definitions[ci];
        invalidMetadata.Inventory.Content.Definitions[ci] = cd with { Wear = cd.Wear! with { OverlapTags = [""] } };
        try { InventoryContentValidation.Validate(invalidMetadata); throw new Exception("Invalid overlap metadata accepted"); }
        catch (ArgumentException) { Check(true, "invalid authored overlap tags reject at content validation"); }

        var outer = new SimulationWorld(map, config); state = outer.Inventory;
        var suitIndex = Array.FindIndex(state.Content!.Definitions, d => d.Id == "chem-suit");
        var suitDef = state.Content.Definitions[suitIndex];
        state.Content.Definitions[suitIndex] = suitDef with { Wear = suitDef.Wear! with { Layer = 6 } };
        state.Items["chemical-suit"] = state.Items["chemical-suit"] with { Placement = new("person:avatar", "suit") };
        try { InventoryPlacement.Validate(state); throw new Exception("Covered outermost pack accepted"); }
        catch (ArgumentException ex) { Check(ex.Message.Contains("outermost"), "a higher numeric layer cannot cover an outermost pack"); }

        var combat = Sim(); combat.Player.Facing = 1; Check(combat.ArmTraining(), "clothing combat fixture could not start");
        combat.Combat.Cursor = combat.Combat.Roster.FindIndex(c => c.ActorId == combat.Player.Id);
        combat.Combat.Roster[combat.Combat.Cursor] = combat.Combat.Roster[combat.Combat.Cursor] with { Ap = 10 };
        var quote = combat.QuoteInventory(close);
        Check(combat.HandleInventory(close) && combat.Combat.For(combat.Player.Id)!.Ap == 10 - quote.Ap,
            "fastening consumes authored AP exactly once");
        combat.Tick(); saved = combat.Save(); clone = Sim(); clone.Restore(saved);
        Check(clone.Save() == saved, "paid fastening and its tactical continuation restore together");
        Resolve(combat);
        Check(!Item(combat, "avatar-coat").GarmentOpen && !combat.OscpAccess(OscpDevice.Phone).Available,
            "paid fastening applies physical coverage only when resolved");
        Console.WriteLine($"WEAR_CHECKS_PASS: {passed} assertions; layers, coverage, fastening, dressing dependencies, front/back packs and persistence.");
        return passed;
    }
}
