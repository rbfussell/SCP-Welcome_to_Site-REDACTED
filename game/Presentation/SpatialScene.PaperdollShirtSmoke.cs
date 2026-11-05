using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunPaperdollShirtSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface; var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Shirt sprite: " + reason); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while ((sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0) && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid work timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest r) { sim.ManualPause = false; Check(sim.HandleInventory(r), sim.LastMessage); await Finish(); }
        void Click(Vector2 at, bool twice = false, MouseButton button = MouseButton.Left) { Input.WarpMouse(at); InventoryClick(at, twice, button); }
        Vector2 Target(string region)
        {
            var b = proof.Art.Bounds(surface.Size);
            for (var y = .30f; y < .42f; y += .003f)
            for (var x = .26f; x < .75f; x += .003f)
            {
                var global = (surface.GlobalPosition + b.Position + new Vector2(x, y) * b.Size).Round(); var local = global - surface.GlobalPosition;
                if (new[] { Vector2.Zero, Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down }.All(offset =>
                    proof.Art.StorageTarget(sim, "front", surface.Size, local + offset) is { Owner: "avatar-shirt" } p && p.Region == region)) return global;
            }
            throw new InvalidOperationException("Shirt sprite: missing contour " + region);
        }
        Vector2 Source(string id)
        {
            var p = sim.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement; var g = panel.FindGrid(p.ParentId, p.RegionId);
            return g.GlobalPosition + new Vector2(1 + p.X * g.CellPixels + 8, 44 + p.Y * g.CellPixels + 8);
        }
        await Act(sim.GroundDropRequest("avatar-backpack")); await Act(sim.GroundDropRequest("avatar-coat"));
        foreach (var region in new[] { "chest-left", "chest-right" })
        {
            var at = Target(region); var before = sim.Save(); Input.WarpMouse(at);
            using (var motion = new InputEventMouseMotion { Position = at, GlobalPosition = at }) GetViewport().PushInput(motion);
            await InventoryFrames(4);
            Check(surface.HoveredPocket?.Region == region && sim.Save() == before, "hover is not read-only or contour-local");
            await InventoryCapture("-shirt-" + region);
            Click(at, true); await InventoryFrames(); Check(sim.Save() == before && !sim.Inventory.IsOpen("avatar-shirt", region), "paused opening succeeded");
            sim.ManualPause = false; Click(at, true); Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Open, "buttoned pocket bypassed paid opening"); await Finish();
            Check(panel.Browser.CurrentContents == ("avatar-shirt", region), "wrong shirt compartment opened");
            Click(at, button: MouseButton.Right); await InventoryFrames();
            var menu = panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>();
            Check(menu.Any(b => b.Text == "Button"), "shirt pocket lost button menu"); Click(new(600, 55)); await InventoryFrames();
        }
        await Act(sim.GroundDropRequest("spare-dressing"));
        await InventoryDrag(Source("spare-dressing"), Target("chest-right"), async () =>
        { Check(surface.StoragePreview?.Allowed == true && surface.StorageTarget?.Region == "chest-right", "drop missed right shirt pocket"); await InventoryCapture("-shirt-storage"); sim.ManualPause = false; }); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement is { ParentId: "avatar-shirt", RegionId: "chest-right" }, "stored elsewhere");
        await Act(sim.GroundDropRequest("personnel-badge"));
        var mount = proof.Art.MountTargets(sim, "front").Single(t => t.Owner == "avatar-shirt" && t.Region == "badge-clip");
        var bounds = proof.Art.Bounds(surface.Size); var clip = surface.GlobalPosition + bounds.Position + mount.Origin * bounds.Size;
        await InventoryDrag(Source("personnel-badge"), clip, async () =>
        { Check(surface.DropPreview?.Quote.Allowed == true && surface.DropPreview.Request.Destination?.RegionId == "badge-clip", "badge clip target refused correct item"); await InventoryCapture("-shirt-badge-mount"); sim.ManualPause = false; }); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "personnel-badge").Placement is { ParentId: "avatar-shirt", RegionId: "badge-clip" }, "badge did not attach to shirt");
        var badgeAt = clip + new Vector2(0, 7);
        Check(surface.Pick(badgeAt - surface.GlobalPosition) == "personnel-badge", "badge pixels lost separate identity");
        await InventoryCapture("-shirt-badge-worn");
        await Act(new(InventoryActionKind.Move, "avatar-coat", new("person:avatar", "outerwear")));
        await Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(!sim.InventoryMoveAccess("personnel-badge").Allowed && surface.Pick(badgeAt - surface.GlobalPosition) != "personnel-badge", "covered badge remained accessible or visible");
        foreach (var region in new[] { "chest-left", "chest-right" }) Check(sim.InventoryPocketCoverage("avatar-shirt", region)?.Covered == true, "coat failed to cover pocket");
        await InventoryCapture("-shirt-badge-covered");
        await Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        Check(sim.InventoryMoveAccess("personnel-badge").Allowed, "unzip failed to restore badge contact access");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        GD.Print("PAPERDOLL_SHIRT_SMOKE_PASS: two real buttoned contours, paid opening/storage, native badge clip drop, separate worn identity and jacket coverage.");
    }
}
