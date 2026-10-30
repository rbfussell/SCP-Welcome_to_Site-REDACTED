using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunOverlaySmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory;
        sim.ManualPause = true; var original = sim.Save(); sim.Player.Cell = new(13, 6);
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Overlay UI: " + reason); }
        async Task Act(InventoryRequest request)
        {
            sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage);
            for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid overlay work did not resolve"); sim.ManualPause = true; await InventoryFrames();
        }
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("fixture:crate-a", "store", 0, 8)));
        await Act(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt")));
        await Act(new(InventoryActionKind.Move, "utility-belt-2", new("person:avatar", "belt", BodyPose: new(-4, 10, 2))));
        await Act(new(InventoryActionKind.Move, "utility-belt-3", new("person:avatar", "belt", BodyPose: new(-4, 8))));
        Check(sim.InventoryWornCells.Any(c => c.ItemId == "utility-belt-2" && c.SubLayer == 2), "crossing did not create a second sublayer");
        // Real pointer preview on the lower belt: the crossed contact is unavailable, the source remains in hand.
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("person:avatar", "hand")));
        panel.Inspect("utility-belt"); await InventoryFrames();
        var surface = panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        var hand = panel.FindGrid("person:avatar", "hand");
        ((ScrollContainer)hand.GetParent().GetParent()).EnsureControlVisible(hand); await InventoryFrames();
        var before = sim.Save();
        await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), surface.MountPosition(new(0, 0)), async () =>
        {
            Check(surface.MountPreview is { Handling.Allowed: false } preview && preview.Handling.Reason.Contains("contact", StringComparison.OrdinalIgnoreCase),
                "covered mounting contact did not produce a rejected native preview");
            Check(sim.Save() == before, "contact preview performed physical work");
            await InventoryCapture("-covered-mount");
        });
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-pouch").Placement.RegionId == "hand", "rejected drop moved the pouch");
        await Act(new(InventoryActionKind.Move, "utility-pouch", new("fixture:crate-a", "store", 10, 9)));
        await Act(new(InventoryActionKind.Move, "utility-belt-4", new("person:avatar", "hand")));
        panel.Select("utility-belt-4"); InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        panel.BodyFit.Choose("utility-belt-4"); await InventoryFrames();
        InventoryClick(panel.BodyFit.Surface.CellPosition(new(-4, 8))); await InventoryFrames();
        Check(panel.BodyFit.Preview is { Geometry.Fits: true, Handling.Allowed: false } blocked
            && blocked.Handling.Reason.Contains("Overlay limit") && panel.BodyFit.ApplyButton.Disabled,
            "a geometrically valid fourth-item crossing failed to explain its band rejection");
        await InventoryCapture("-overlay-limit");
        panel.BodyFit.Choose("utility-belt-2"); await InventoryFrames();
        await InventoryCapture("-crossed-straps");
        InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        if (panel.EngineeringSlots.ButtonPressed) { InventoryClick(panel.EngineeringSlots.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        var row = panel.EquipmentList.Row("utility-belt-2");
        Check(row >= 0 && panel.EquipmentList.GetItemText(row).Contains('_'), "equipment list does not expose actual layer_N labels");
        await InventoryCapture("-sublayer-list");
        var saved = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(saved); Check(clone.Save() == saved, "crossed outfit failed exact save restore");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open();
        if (!panel.EngineeringSlots.ButtonPressed) { InventoryClick(panel.EngineeringSlots.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        sim.ManualPause = false;
        GD.Print("OVERLAY_SMOKE_PASS: native covered-contact preview, paid crossed straps, blocked fourth intersection, sublayer list, body diagnostics and persistence.");
    }
}
