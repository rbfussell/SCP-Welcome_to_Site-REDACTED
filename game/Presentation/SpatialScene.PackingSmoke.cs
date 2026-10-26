using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunPackingSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory;
        sim.ManualPause = true; var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Packing UI: " + reason); }
        async Task Finish()
        {
            for (var i = 0; i < 900 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "handling did not finish"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Move(string id, string parent, string region, int x = 0, int y = 0)
        {
            sim.ManualPause = false;
            Check(sim.HandleInventory(new(InventoryActionKind.Move, id, new(parent, region, x, y))), sim.LastMessage); await Finish();
        }
        InventoryGearSurface Surface() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        Button Action(string label) => panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0)
            .GetChildren().OfType<Button>().Single(b => b.Text == label);
        await Move("gum-packet", "avatar-backpack", "main", 4, 0);
        panel.Inspect("avatar-backpack"); panel.OpenCompartment("avatar-backpack", "main"); await InventoryFrames();
        var grid = panel.FindGrid("avatar-backpack", "main");
        var safe = sim.Save();
        await InventoryDrag(grid.GlobalPosition + new Vector2(130, 59), grid.GlobalPosition + new Vector2(20, 60), async () =>
        {
            Check(panel.DropPreview is { IntoContainer: true, Request.Kind: InventoryActionKind.AutoPlace, Quote.Allowed: true }, "nested case was not a container-target drop");
            Check(sim.Save() == safe, "hover mutated the graph"); await InventoryCapture("-container-drop");
        });
        Check(sim.Save() == safe, "paused drop performed free work");
        sim.ManualPause = false;
        await InventoryDrag(grid.GlobalPosition + new Vector2(130, 59), grid.GlobalPosition + new Vector2(20, 60));
        Check(sim.Inventory.Pending is { Request.Kind: InventoryActionKind.AutoPlace, Packing.Moves.Length: 1 }, "native drop did not schedule quoted packing");
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "gum-packet").Placement == new ItemPlacement("medical-case", "inside", 2, 0, true), "case failed to use rotated free space");
        // Blank grid, contained item, and external pouch all expose the same scoped Arrange command.
        InventoryClick(grid.GlobalPosition + new Vector2(200, 270), button: MouseButton.Right); await InventoryFrames();
        Check(Action("Arrange " + sim.Inventory.Region("avatar-backpack", "main").Name) is not null, "blank grid lacks Arrange");
        InventoryClick(grid.GlobalPosition + new Vector2(20, 60), button: MouseButton.Right); await InventoryFrames();
        Check(Action("Arrange " + sim.Inventory.Region("avatar-backpack", "main").Name) is not null, "contained item lacks parent-compartment Arrange");
        InventoryClick(Surface().HotspotPosition("main"), button: MouseButton.Right); await InventoryFrames();
        Check(Action("Arrange " + sim.Inventory.Region("avatar-backpack", "main").Name) is not null, "external pouch lacks Arrange");
        InventoryClick(Action("Arrange " + sim.Inventory.Region("avatar-backpack", "main").Name).GetGlobalRect().GetCenter()); await InventoryFrames();
        await Move("medical-case", "avatar-backpack", "main", 4, 5);
        InventoryClick(grid.GlobalPosition + new Vector2(20, 60), button: MouseButton.Right); await InventoryFrames();
        sim.ManualPause = false; InventoryClick(Action("Arrange " + sim.Inventory.Region("avatar-backpack", "main").Name).GetGlobalRect().GetCenter());
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Arrange, "explicit native Arrange did not create timed work");
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "medical-case").Placement == new ItemPlacement("avatar-backpack", "main"), "Arrange did not compact one compartment");
        await Move("flashlight", "person:avatar", "hand");
        var hand = panel.FindGrid("person:avatar", "hand");
        ((ScrollContainer)hand.GetParent().GetParent()).EnsureControlVisible(hand); await InventoryFrames();
        sim.ManualPause = false;
        await InventoryDrag(hand.GlobalPosition + new Vector2(20, 70), Surface().HotspotPosition("side"));
        Check(sim.Inventory.Pending is { Request.Kind: InventoryActionKind.AutoPlace }, "pouch-shaped gear surface did not accept native drop");
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "flashlight").Placement.ParentId == "avatar-backpack", "gear surface drop failed");
        await InventoryCapture("-packing-complete");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); sim.ManualPause = false;
        GD.Print("PACKING_SMOKE_PASS: exact manual placement preserved; native nested-container and segmented-pouch drops, scoped Arrange menus, paid handling and paused rejection.");
    }
}
