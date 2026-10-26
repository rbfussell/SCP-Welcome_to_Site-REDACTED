using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunBodyFitSmoke()
    {
        var panel = _hud.Inventory; var sim = _simulation;
        sim.ManualPause = true; var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Body fit UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 600 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "fitting did not finish"); sim.ManualPause = true; await InventoryFrames();
        }
        sim.ManualPause = false;
        Check(sim.HandleInventory(new(InventoryActionKind.Move, "avatar-shoes", new("person:avatar", "hand"))), "prepare real held boots"); await Finish();
        var hand = panel.FindGrid("person:avatar", "hand");
        ((ScrollContainer)hand.GetParent().GetParent()).EnsureControlVisible(hand); await InventoryFrames();
        InventoryClick(hand.GlobalPosition + new Vector2(30, 80)); await InventoryFrames();
        Check(panel.SelectedItem == "avatar-shoes", "native item selection failed");
        var safe = sim.Save(); InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        var fit = panel.BodyFit;
        Check(fit.Visible && fit.Preview is { Geometry.Fits: true, Handling.Allowed: true }, "body fit view failed to use selected clothing");
        Check(sim.Save() == safe, "opening body mode changed physical state");
        var defaultPose = fit.Draft;
        await InventoryCapture("-body-fit-valid");
        InventoryClick(fit.Surface.CellPosition(new(0, 1))); await InventoryFrames();
        Check(fit.Preview is { Geometry.Fits: false } && fit.ApplyButton.Disabled && sim.Save() == safe, "invalid anchor was accepted or altered state");
        await InventoryCapture("-body-fit-invalid");
        var startRotation = fit.Draft.Rotation;
        for (var i = 0; i < 6; i++) { InventoryKey(Key.R); await InventoryFrames(1); }
        Check(fit.Draft.Rotation == startRotation && !panel.Rotated && sim.Save() == safe, "six body rotations affected storage orientation or gameplay");
        var defaults = fit.GetChildren().OfType<HBoxContainer>().Single().GetChildren().OfType<Button>().Single(b => b.Text == "Default");
        InventoryClick(defaults.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(fit.Draft == defaultPose && !fit.ApplyButton.Disabled, "Default did not restore the valid body anchor");
        InventoryClick(fit.ApplyButton.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(sim.Save() == safe && sim.Inventory.Pending is null, "paused apply supplied free fitting");
        sim.ManualPause = false; InventoryClick(fit.ApplyButton.GetGlobalRect().GetCenter());
        Check(sim.Inventory.Pending is { Request.Destination.BodyPose: not null }
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "avatar-shoes").Placement.RegionId == "hand", "Apply failed to begin paid equipment work");
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "avatar-shoes").Placement.BodyPose == defaultPose, "paid fit did not commit body coordinates");
        await InventoryCapture("-body-fit-applied");
        InventoryClick(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(!fit.Visible && panel.FindGrid("person:avatar", "hand").IsVisibleInTree(), "returning to slots lost existing paperdoll controls");
        var saved = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(saved); Check(clone.Save() == saved, "body-fitted equipment failed to restore with the ordinary save");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("BODY_FIT_SMOKE_PASS: native body-mode toggle, real item selection, mask/contact preview, invalid click, six rotations, default reset, paused rejection and paid fitting/save.");
    }
}
