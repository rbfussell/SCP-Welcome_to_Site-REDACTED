using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunBodyInspectionSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory;
        sim.ManualPause = true; var original = sim.Save(); sim.Player.Cell = new(13, 6);
        void Click(Vector2 at, bool doubleClick = false, MouseButton button = MouseButton.Left)
        { Input.WarpMouse(at); InventoryClick(at, doubleClick, button); }
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Body inspection UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "ordinary paid work did not resolve"); sim.ManualPause = true; await InventoryFrames();
        }
        sim.ManualPause = false;
        Check(sim.HandleInventory(new(InventoryActionKind.Move, "utility-belt", new("person:avatar", "belt"))), sim.LastMessage); await Finish();
        Click(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        var fit = panel.BodyFit; var surface = fit.Surface; var chest = new BodyHex(-4, 8); var waist = new BodyHex(-6, 12);
        Click(fit.InspectToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(fit.InspectWorn && fit.ViewSurface == "front" && fit.ApplyButton.Disabled, "inspection mode did not separate picking from fitting");
        var before = sim.Save();
        Click(surface.CellPosition(chest)); await InventoryFrames();
        await InventoryCapture("-body-inspect-first-click");
        Check(panel.SelectedItem == "avatar-shirt" && surface.HoveredItem == "avatar-shirt" && sim.Save() == before,
            $"front opening failed: selected={panel.SelectedItem}, hovered={surface.HoveredItem}, unchanged={sim.Save() == before}");
        Click(surface.CellPosition(chest), true); await InventoryFrames();
        Check(panel.Browser.CurrentGear == "avatar-shirt", "body double-click did not inspect the actual worn instance");
        Click(surface.CellPosition(waist), button: MouseButton.Right); await InventoryFrames();
        var menu = panel.GetChildren().OfType<PanelContainer>().Last();
        Check(menu.GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Any(b => b.Text == "Remove → hand") && panel.SelectedItem == "utility-belt",
            "body context menu did not address the picked item");
        Click(panel.GlobalPosition + new Vector2(80, 80)); await InventoryFrames();
        await InventoryCapture("-body-inspect-front");
        var left = new BodyHex(-6, 8); var frontX = surface.CellPosition(left).X;
        Click(fit.FlipButton.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(fit.ViewSurface == "back" && frontX < surface.GetGlobalRect().GetCenter().X
            && surface.CellPosition(left).X > surface.GetGlobalRect().GetCenter().X && sim.Save() == before,
            "back view did not mirror screen projection without changing physical state");
        Click(surface.CellPosition(chest), true); await InventoryFrames();
        Check(panel.Browser.CurrentGear == "avatar-backpack" && surface.HoveredItem == "avatar-backpack", "back view picked through the worn pack");
        await InventoryCapture("-body-inspect-back");
        var gear = panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        Click(gear.HotspotPosition("main"), true); await InventoryFrames();
        var grid = panel.FindGrid("avatar-backpack", "main");
        Click(fit.FlipButton.GetGlobalRect().GetCenter()); await InventoryFrames();
        var destination = grid.GlobalPosition + new Vector2(15, 44 + 3 * grid.CellPixels + 15);
        before = sim.Save();
        await InventoryDrag(surface.CellPosition(chest), destination, async () =>
        { Check(panel.DragItemId is null && sim.Inventory.Pending is null, "visible but restrained shirt became a movable source"); await InventoryFrames(); });
        Check(sim.Save() == before, "blocked source drag changed equipment");
        await InventoryDrag(surface.CellPosition(waist), destination, async () =>
        {
            Check(panel.DragItemId == "utility-belt" && panel.DropPreview is { Quote.Allowed: true } preview
                && preview.Request.Destination == new ItemPlacement("avatar-backpack", "main", 0, 3), "body drag did not use the exact ordinary storage target");
            Check(sim.Save() == before, "body-source hover performed work"); await InventoryCapture("-body-source-valid");
        });
        Check(sim.Save() == before && sim.Inventory.Pending is null, "paused removal supplied free undressing");
        sim.ManualPause = false;
        await InventoryDrag(surface.CellPosition(waist), destination);
        Check(sim.Inventory.Pending?.Request.ItemId == "utility-belt" && sim.InventoryBodyPick("front", waist)?.ItemId == "utility-belt",
            "source did not remain worn while paid handling began");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "native body-source continuation failed exact restore");
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "utility-belt").Placement == new ItemPlacement("avatar-backpack", "main", 0, 3)
            && sim.InventoryBodyPick("front", waist)?.ItemId == "avatar-trousers", "completed storage differs from preview or stale belt remains targetable");
        await InventoryCapture("-body-source-applied");
        panel.Close(); panel.Open(); Click(panel.BodyFitToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(!fit.InspectWorn && fit.ViewSurface == "front", "reopening inventory retained transient inspection state");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("BODY_INSPECTION_SMOKE_PASS: native exposed-item selection, double-click/context, front/back mirroring, restrained source refusal, paused/timed body-to-storage drag and continuation.");
    }
}
