using Godot;
using System.Threading.Tasks;
using SiteSimulation;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunWearSmoke()
    {
        var panel = _hud.Inventory; var sim = _simulation;
        sim.ManualPause = true; var original = sim.Save();
        async Task Finish()
        {
            for (var i = 0; i < 300 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            if (sim.Inventory.Pending is not null) throw new InvalidOperationException("Garment UI action failed to resolve.");
            sim.ManualPause = true; await InventoryFrames();
        }
        void Click(Button button) => InventoryClick(button.GetGlobalRect().GetCenter());
        panel.Inspect("avatar-trousers"); panel.OpenCompartment("avatar-trousers", "left"); panel.Inspect("gum-packet"); await InventoryFrames();
        var coat = panel.FindGrid("person:avatar", "outerwear");
        InventoryClick(coat.GlobalPosition + new Vector2(30, 80)); await InventoryFrames();
        if (panel.GarmentToggle.Disabled) throw new InvalidOperationException("Selecting a supported garment did not enable its physical opening control.");
        var held = sim.Save(); Click(panel.GarmentToggle); await InventoryFrames();
        if (sim.Save() != held) throw new InvalidOperationException("Fastening during pause supplied a free clothing change.");
        sim.ManualPause = false; Click(panel.GarmentToggle);
        if (sim.Inventory.Pending?.Request.Kind != InventoryActionKind.CloseGarment) throw new InvalidOperationException("Actual fastening button did not begin timed work.");
        await Finish();
        if (panel.InspectedContainers.Contains("gum-packet") || sim.InventoryAccess("avatar-trousers", "left").Allowed
            || sim.InventoryContents("person:avatar", "torso").Count != 0)
            throw new InvalidOperationException("Fastened coat failed to hide its inner shirt/pocket paths and descendant views.");
        held = sim.Save();
        // The main equipment list replaces the old fixed-layer selector; covered rows remain read-only.
        Click(panel.EngineeringSlots); await InventoryFrames();
        var list = panel.EquipmentList;
        var shirtRow = list.Row("avatar-shirt");
        if (shirtRow < 0 || !list.GetItemText(shirtRow).Contains("0") || !list.GetItemText(shirtRow).Contains("covered"))
            throw new InvalidOperationException("Equipment list omitted the covered shirt's real layer.");
        InventoryClick(list.GlobalPosition + list.GetItemRect(shirtRow).GetCenter()); await InventoryFrames();
        if (panel.SelectedItem != "avatar-shirt" || sim.Save() != held)
            throw new InvalidOperationException("Native equipment row selection changed physical state or selected the wrong item.");
        InventoryClick(list.GlobalPosition + list.GetItemRect(shirtRow).GetCenter(), true); await InventoryFrames();
        if (panel.Browser.CurrentGear == "avatar-shirt" || sim.Save() != held)
            throw new InvalidOperationException("Double-clicking covered equipment bypassed inspection access.");
        await InventoryCapture("-equipment-layers");
        Click(panel.EngineeringSlots); await InventoryFrames();
        if (!sim.InventoryEquipment.Any(e => e.ItemId == "avatar-shirt" && !e.Visible && e.Layers.Any(l => l.Layer == 0)))
            throw new InvalidOperationException("Known covered shirt is missing from actual layer projection.");
        panel.Inspect("gum-packet");
        if (panel.InspectedContainers.Contains("gum-packet") || sim.Save() != held)
            throw new InvalidOperationException("Layer inspection uncovered physical storage or mutated the save.");
        var shirt = panel.FindGrid("person:avatar", "torso");
        ((ScrollContainer)shirt.GetParent().GetParent()).EnsureControlVisible(shirt); await InventoryFrames();
        InventoryClick(shirt.GlobalPosition + new Vector2(30, 80));
        if (panel.SelectedItem != "avatar-shirt" || shirt._GetDragData(new Vector2(30, 80)).VariantType != Variant.Type.Nil)
            throw new InvalidOperationException("A known covered shirt could not be inspected or incorrectly became draggable.");
        InventoryClick(panel.TakeInHand.GetGlobalRect().GetCenter());
        if (sim.Save() != held) throw new InvalidOperationException("Layer-view selection bypassed physical dressing rules.");
        await InventoryCapture("-layer-inspection");
        ((ScrollContainer)coat.GetParent().GetParent()).EnsureControlVisible(coat); await InventoryFrames();
        InventoryClick(coat.GlobalPosition + new Vector2(30, 80)); await InventoryFrames();
        await InventoryCapture("-coat-fastened");
        sim.ManualPause = false; Click(panel.GarmentToggle); await Finish();
        if (!sim.InventoryAccess("avatar-trousers", "left").Allowed || sim.InventoryContents("person:avatar", "torso").Count != 1)
            throw new InvalidOperationException("Opening a coat failed to expose the underlying paperdoll and storage.");
        await InventoryCapture("-coat-open");
        sim.Player.Cell = new(13, 6); panel.Inspect("fixture:crate-a"); panel.OpenCompartment("fixture:crate-a", "store"); await InventoryFrames();
        var crate = panel.FindGrid("fixture:crate-a", "store");
        void SelectSuit() => InventoryClick(crate.GlobalPosition + new Vector2(6 * crate.CellPixels + 15, 59));
        SelectSuit();
        if (panel.SelectedItem != "chemical-suit") throw new InvalidOperationException("The physical suit could not be selected from the nearby crate.");
        held = sim.Save(); sim.ManualPause = false; var beforeAttempt = sim.Save(); Click(panel.WearDefault);
        if (sim.Inventory.Pending is not null || sim.Save() != beforeAttempt || !sim.LastMessage.Contains("backpack", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The worn pack did not block fitting the suit through the real UI.");
        var pack = panel.FindGrid("person:avatar", "back");
        InventoryClick(pack.GlobalPosition + new Vector2(30, 80)); Click(panel.TakeInHand); await Finish();
        sim.ManualPause = false; SelectSuit(); Click(panel.WearDefault); await Finish();
        if (sim.Inventory.DiagnosticItems.Single(i => i.Id == "chemical-suit").Placement.RegionId != "suit")
            throw new InvalidOperationException("The suit was not fitted after clearing the pack.");
        var hand = panel.FindGrid("person:avatar", "hand");
        InventoryClick(hand.GlobalPosition + new Vector2(30, 80)); sim.ManualPause = false; Click(panel.WearDefault); await Finish();
        if (sim.Inventory.DiagnosticItems.Single(i => i.Id == "avatar-backpack").Placement.RegionId != "back"
            || sim.InventoryAccess("avatar-trousers", "left").Allowed)
            throw new InvalidOperationException("Refitting a pack failed or incorrectly uncovered inner suit equipment.");
        var suitGrid = panel.FindGrid("person:avatar", "suit");
        ((ScrollContainer)suitGrid.GetParent().GetParent()).EnsureControlVisible(suitGrid); await InventoryFrames();
        await InventoryCapture("-suit-fitted");
        var saved = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(saved);
        if (clone.Save() != saved) throw new InvalidOperationException("Dressed paperdoll and coverage failed to persist.");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("WEAR_SMOKE_PASS: actual paid garment controls, paused rejection, covered pocket/descendant hiding, exposed shirt, explicit pack-off/suit-on/pack-on buttons and save restore.");
    }
}
