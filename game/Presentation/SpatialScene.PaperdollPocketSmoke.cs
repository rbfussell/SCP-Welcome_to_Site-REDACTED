using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunPaperdollPocketSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface; var original = sim.Save();
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Paperdoll pocket: " + message); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while ((sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0) && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid work timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        Vector2 Target()
        {
            var b = proof.Art.Bounds(surface.Size);
            for (var y = .28f; y < .43f; y += .003f)
            for (var x = .22f; x < .48f; x += .003f)
            {
                var global = (surface.GlobalPosition + b.Position + new Vector2(x, y) * b.Size).Round(); var at = global - surface.GlobalPosition;
                if (new[] { Vector2.Zero, Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down }.All(offset =>
                    proof.Art.StorageTarget(sim, "front", surface.Size, at + offset) is { Owner: "avatar-coat", Region: "chest", Pocket: not null })) return global;
            }
            throw new InvalidOperationException("Paperdoll pocket: missing stable chest segment");
        }
        void Click(Vector2 at, bool twice = false, MouseButton button = MouseButton.Left) { Input.WarpMouse(at); InventoryClick(at, twice, button); }
        await Act(sim.GroundDropRequest("avatar-backpack"));
        if (sim.Inventory.IsOpen("avatar-coat", "chest")) await Act(new(InventoryActionKind.Close, "avatar-coat", RegionId: "chest"));
        var target = Target(); var before = sim.Save(); Input.WarpMouse(target); await InventoryFrames();
        Check(surface.HoveredPocket is { Owner: "avatar-coat", Region: "chest" } && sim.Save() == before, "hover lost real pocket or did physical work");
        await InventoryCapture("-paperdoll-pocket-hover");
        Click(target, true); await InventoryFrames();
        Check(sim.Inventory.Pending is null && !sim.Inventory.IsOpen("avatar-coat", "chest") && sim.Save() == before, "paused double-click opened pocket");
        sim.ManualPause = false; Click(target, true);
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Open && !sim.Inventory.IsOpen("avatar-coat", "chest"), "double-click bypassed paid opening");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
        clone.Restore(pending); Check(clone.Save() == pending, "paid pocket opening did not restore exactly"); await Finish();
        Check(panel.Browser.CurrentGear == "avatar-coat" && panel.Browser.CurrentContents == ("avatar-coat", "chest"), "pocket did not open its normal gear/contents trail");
        Click(target, button: MouseButton.Right); await InventoryFrames();
        var menu = panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>();
        Check(menu.Any(b => b.Text == "Button") && !menu.Any(b => b.Text == "Zip"), "pocket menu targeted garment zipper instead of its own fastening");
        Click(new(600, 55)); await InventoryFrames();
        await Act(sim.GroundDropRequest("spare-dressing"));
        var item = sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing"); var grid = panel.FindGrid(item.Placement.ParentId, item.Placement.RegionId);
        var source = grid.GlobalPosition + new Vector2(1 + item.Placement.X * grid.CellPixels + 8, 44 + item.Placement.Y * grid.CellPixels + 8);
        await InventoryDrag(source, target, async () =>
        {
            Check(surface.StorageTarget is { Owner: "avatar-coat", Region: "chest", Pocket: not null } && surface.StoragePreview is { Allowed: true }, "pocket drop missed its exact compartment");
            await InventoryCapture("-paperdoll-pocket-drop"); sim.ManualPause = false;
        }); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement.ParentId == "avatar-coat", "pocket transfer went elsewhere");
        await Act(new(InventoryActionKind.CloseGarment, "avatar-coat")); target = Target();
        Check(sim.Inventory.IsOpen("avatar-coat", "chest"), "zipping jacket changed pocket fastening");
        await Act(new(InventoryActionKind.OpenGarment, "avatar-coat")); target = Target();
        Check(proof.Art.StorageTarget(sim, "back", surface.Size, target - surface.GlobalPosition)?.Region != "chest", "front pocket was reachable through back art");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "front")));
        Check(proof.Art.StorageTarget(sim, "front", surface.Size, target - surface.GlobalPosition)?.Owner != "avatar-coat", "opaque front pack exposed a hidden chest segment");
        await InventoryCapture("-paperdoll-pocket-covered");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        await RunPaperdollTrouserPocketSmoke();
        await RunPaperdollPackPocketSmoke();
        await RunPaperdollShirtSmoke();
        GD.Print("PAPERDOLL_POCKET_SMOKE_PASS: source-local chest segment, alpha hover, paid double-click/restore, correct fastening menu, exact storage, both jacket states, rear exclusion and front-pack occlusion.");
    }
}
