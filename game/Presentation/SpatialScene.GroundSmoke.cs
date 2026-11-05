using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunGroundSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var original = sim.Save();
        panel.Close(); panel.Open(); sim.ManualPause = true; sim.Player.Cell = new(13, 6); await InventoryFrames();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Ground UI: " + reason); }
        ItemPlacement Placement(string id) => sim.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement;
        InventoryGrid Floor() => panel.GroundView.Grid!;
        Vector2 Spot(int x = 0, int y = 0) => Floor().GlobalPosition + new Vector2(1 + (x + .5f) * Floor().CellPixels, 44 + (y + .5f) * Floor().CellPixels);
        async Task Finish()
        {
            for (var i = 0; i < 1000 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid floor handling timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        async Task<InventoryGrid> Slot(string slot)
        {
            var grid = panel.FindGrid("person:avatar", slot);
            ((ScrollContainer)grid.GetParent().GetParent()).EnsureControlVisible(grid); await InventoryFrames(); return grid;
        }
        async Task Drop(string id, string slot)
        {
            var source = await Slot(slot); var before = sim.Save();
            await InventoryDrag(source.GlobalPosition + new Vector2(30, 80), Spot(), async () =>
            {
                Check(panel.DropPreview is { IntoContainer: false, Quote.Allowed: true } p && p.Grid == Floor()
                    && p.Request.Destination?.ParentId == Floor().ParentId, "floor drag did not preview an exact pile placement");
                Check(sim.Save() == before, "floor preview changed state"); sim.ManualPause = false;
            });
            Check(sim.Inventory.Pending?.Request.ItemId == id && Placement(id).ParentId == "person:avatar", "drop skipped paid source retention");
            await Finish(); Check(Placement(id).ParentId == Floor().ParentId, "floor drop never committed");
        }
        async Task Wear(string id, string slot)
        {
            var target = await Slot(slot); var before = Placement(id);
            await InventoryDrag(Spot(), target.GlobalPosition + new Vector2(15, 60), () => { sim.ManualPause = false; return Task.CompletedTask; });
            Check(sim.Inventory.Pending?.Request.ItemId == id && Placement(id) == before, "pickup bypassed paid source retention");
            await Finish(); Check(Placement(id).RegionId == slot && Placement(id).GroundOrder is null, "floor pickup lost its real worn destination");
        }
        Check(panel.GroundView.IsVisibleInTree() && GetViewportRect().Encloses(Floor().GetGlobalRect()), "permanent floor grid is missing or clipped");
        Check(panel.Browser.CurrentGear is null && panel.Browser.CurrentContents is null, "floor dock opened inspection trails");
        await Act(new(InventoryActionKind.Move, "spare-dressing", new("person:avatar", "hand")));
        var held = Placement("spare-dressing"); var groundId = Floor().ParentId;
        await Drop("avatar-backpack", "back"); await Drop("avatar-coat", "outerwear"); await Drop("avatar-shirt", "torso");
        Check(Floor().DisplayedItems.Select(i => i.Id).SequenceEqual(new[] { "avatar-backpack", "avatar-coat", "avatar-shirt" }), "floor grid does not retain the ordered clothes pile");
        InventoryClick(Spot()); await InventoryFrames(); Check(panel.SelectedItem == "avatar-shirt", "floor picker did not select the top item");
        var safe = sim.Save(); panel.Inspect("avatar-backpack"); await InventoryFrames();
        Check(panel.Browser.CurrentGear is null && sim.Save() == safe && !sim.InventoryAccess("avatar-backpack", "main").Allowed,
            "buried bag inspection revealed its contents");
        Check(!sim.HandleInventory(new(InventoryActionKind.Move, "avatar-coat", new("person:avatar", "outerwear"))) && sim.Save() == safe,
            "covered clothing was pulled out of the pile");
        await InventoryCapture("-ground-clothes-pile");
        InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        Check(panel.GroundView.IsVisibleInTree() && GetViewportRect().Encloses(Floor().GetGlobalRect()), "sprite mode hid or clipped the ground dock");
        await InventoryCapture("-ground-sprites-pile");
        InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
        await Wear("avatar-shirt", "torso"); await Wear("avatar-coat", "outerwear"); await Wear("avatar-backpack", "back");
        Check(Placement("spare-dressing") == held && Floor().DisplayedItems.Count == 0 && Placement("medical-case").ParentId == "avatar-backpack",
            "clothes staging borrowed the occupied hand, lost bag contents or left duplicates");
        // A floor bag remains a real inspectable container while destination gear/source trails work normally.
        await Drop("avatar-backpack", "back"); InventoryClick(Spot(), true); await InventoryFrames();
        Check(panel.Browser.CurrentGear == "avatar-backpack", "top floor bag did not open the gear view");
        var gear = panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        InventoryClick(gear.HotspotPosition("main"), true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-backpack", "main"), "floor container did not expose its normal accessible interior");
        var cell = sim.InventoryGroundRoot!.GroundCell!.Value;
        sim.Player.Cell = sim.Map.Cells.First(c => sim.Map.CanTraverse(cell, c) && sim.Actors.All(a => a.Cell != c)); await InventoryFrames();
        Check(Floor().ParentId != groundId && Floor().DisplayedItems.Count == 0 && panel.Browser.CurrentContents is null, "moving carried the floor pile or retained remote contents");
        sim.Player.Cell = cell; await InventoryFrames(); Check(Floor().ParentId == groundId && Floor().DisplayedItems.Single().Id == "avatar-backpack", "returning lost the dropped pack");
        var snapshot = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(snapshot); Check(clone.Save() == snapshot, "floor pile did not save/restore exactly");
        InventoryKey(Key.Escape); await InventoryFrames(); InventoryKey(Key.I); await InventoryFrames();
        Check(panel.GroundView.IsVisibleInTree() && Floor().DisplayedItems.Count == 1 && panel.Browser.CurrentGear is null, "reopening hid the floor or resurrected old inspection trails");
        await InventoryCapture("-ground-persistent-pack");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("GROUND_SMOKE_PASS: permanent current-tile dock, exact stacking drags/top picking, occupied-hand clothes staging and reverse dressing, buried bag access refusal, floor container inspection, sprite mode, tile changes, persistence and interface reopening.");
    }
}
