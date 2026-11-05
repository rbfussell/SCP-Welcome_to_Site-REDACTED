using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Actual sprite input must preserve the existing hip/cargo closure and coverage distinction.</summary>
    private async Task RunPaperdollTrouserPocketSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface;
        var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Trouser pocket: " + reason); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while ((sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0) && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid work timed out");
            sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        Vector2 Target(string region)
        {
            var bounds = proof.Art.Bounds(surface.Size);
            for (var y = .47f; y < .72f; y += .003f)
            for (var x = .15f; x < .85f; x += .003f)
            {
                var global = (surface.GlobalPosition + bounds.Position + new Vector2(x, y) * bounds.Size).Round();
                var at = global - surface.GlobalPosition;
                if (new[] { Vector2.Zero, Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down }.All(offset =>
                    proof.Art.StorageTarget(sim, "front", surface.Size, at + offset) is { Owner: "avatar-trousers", Pocket: not null } t && t.Region == region))
                    return global;
            }
            throw new InvalidOperationException("Trouser pocket: missing stable segment " + region);
        }
        void Click(Vector2 at, bool twice = false, MouseButton button = MouseButton.Left)
        { Input.WarpMouse(at); InventoryClick(at, twice, button); }
        Button[] Menu() => panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().ToArray();

        // Stage the real coat and pack, rather than bypassing garment access or painting through them.
        await Act(sim.GroundDropRequest("avatar-backpack"));
        await Act(sim.GroundDropRequest("avatar-coat"));
        var targets = new Dictionary<string, Vector2>();
        foreach (var region in new[] { "left", "right", "cargo-left", "cargo-right" })
        {
            var target = Target(region); targets.Add(region, target);
            var before = sim.Save(); Input.WarpMouse(target); await InventoryFrames();
            Check(surface.HoveredPocket?.Region == region && sim.Save() == before, "hover changed state or selected another pocket");
            Check(proof.Art.StorageTarget(sim, "back", surface.Size, target - surface.GlobalPosition)?.Region is not ("left" or "right"), "front hip opening leaked into back art");
        }
        foreach (var region in new[] { "left", "right" })
        {
            var before = sim.Save(); Click(targets[region], true); await InventoryFrames();
            Check(panel.Browser.CurrentContents == ("avatar-trousers", region) && sim.Save() == before && sim.Inventory.Pending is null,
                "unsealable hip inspection invented paid opening");
            Click(targets[region], button: MouseButton.Right); await InventoryFrames();
            Check(!Menu().Any(b => b.Text is "Button" or "Unbutton" or "Zip" or "Unzip"), "hip menu invented a fastening");
            var list = panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0);
            Check(!list.GetChildren().OfType<CheckButton>().Any(), "hip menu invented a securing preference");
            Click(new(600, 55)); await InventoryFrames();
        }
        Input.WarpMouse(targets["left"]); await InventoryFrames(); await InventoryCapture("-trouser-hip-hover");
        foreach (var region in new[] { "cargo-left", "cargo-right" })
        {
            if (sim.Inventory.IsOpen("avatar-trousers", region)) await Act(new(InventoryActionKind.Close, "avatar-trousers", RegionId: region));
            var before = sim.Save(); Click(targets[region], true); await InventoryFrames();
            Check(sim.Save() == before && !sim.Inventory.IsOpen("avatar-trousers", region), "paused cargo opening succeeded");
            sim.ManualPause = false; Click(targets[region], true);
            Check(sim.Inventory.Pending?.Request is { Kind: InventoryActionKind.Open, RegionId: var pendingRegion } && pendingRegion == region
                && !sim.Inventory.IsOpen("avatar-trousers", region), "cargo opening bypassed paid handling");
            await Finish();
            Check(panel.Browser.CurrentContents == ("avatar-trousers", region), "cargo opened a different compartment");
            Click(targets[region], button: MouseButton.Right); await InventoryFrames();
            Check(Menu().Any(b => b.Text == "Button"), "cargo menu lost its own buttoned flap");
            Click(new(600, 55)); await InventoryFrames();
        }
        await Act(sim.GroundDropRequest("spare-dressing"));
        var item = sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing");
        var grid = panel.FindGrid(item.Placement.ParentId, item.Placement.RegionId);
        var source = grid.GlobalPosition + new Vector2(1 + item.Placement.X * grid.CellPixels + 8, 44 + item.Placement.Y * grid.CellPixels + 8);
        await InventoryDrag(source, targets["cargo-left"], async () =>
        {
            Check(surface.StorageTarget is { Owner: "avatar-trousers", Region: "cargo-left" } && surface.StoragePreview?.Allowed == true,
                "drop did not address left cargo compartment");
            await InventoryCapture("-trouser-cargo-drop"); sim.ManualPause = false;
        }); await Finish();
        var placed = sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement;
        Check(placed.ParentId == "avatar-trousers" && placed.RegionId == "cargo-left", "drop moved item to another pocket");

        await Act(new(InventoryActionKind.Move, "avatar-coat", new("person:avatar", "outerwear")));
        if (sim.Inventory.DiagnosticItems.Single(i => i.Id == "avatar-coat").GarmentOpen)
            await Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        foreach (var region in new[] { "left", "right" })
        {
            Check(sim.InventoryPocketCoverage("avatar-trousers", region)?.Covered == true && !sim.InventoryAccess("avatar-trousers", region).Allowed,
                "closed coat failed to cover hip mouth");
            var at = targets[region] - surface.GlobalPosition;
            Check(proof.Art.StorageTarget(sim, "front", surface.Size, at)?.Owner != "avatar-trousers", "hidden hip target picked through jacket alpha");
            var before = sim.Save(); panel.OpenCompartment("avatar-trousers", region); await InventoryFrames();
            Check(sim.Save() == before && panel.Browser.CurrentContents != ("avatar-trousers", region), "covered hip contents became accessible");
        }
        foreach (var region in new[] { "cargo-left", "cargo-right" })
        {
            Check(sim.InventoryAccess("avatar-trousers", region).Allowed, "coat blocked lower cargo pocket");
            var target = Target(region); Click(target, true); await InventoryFrames();
            Check(panel.Browser.CurrentContents == ("avatar-trousers", region), "visible cargo pocket failed under closed coat");
        }
        Input.WarpMouse(Target("cargo-left")); await InventoryFrames(); await InventoryCapture("-trouser-coat-coverage");
        await Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Check(new[] { "left", "right" }.All(r => sim.InventoryAccess("avatar-trousers", r).Allowed), "unzip failed to restore physical hip access");
        var snapshot = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(snapshot); Check(clone.Save() == snapshot, "pocket transfers changed continuation compatibility");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        GD.Print("PAPERDOLL_TROUSER_POCKET_SMOKE_PASS: four source-alpha segments, read-only hip inspection, hip/cargo menus, paid cargo opening, direct storage, jacket occlusion/coverage, exposed cargo and continuation.");
    }
}
