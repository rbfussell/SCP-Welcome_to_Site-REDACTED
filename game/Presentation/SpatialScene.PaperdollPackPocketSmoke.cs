using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    /// <summary>Worn bag segments address independent real closures on either mount, without treating harnesses as storage.</summary>
    private async Task RunPaperdollPackPocketSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface; var original = sim.Save();
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Pack pocket: " + message); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while ((sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0) && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid work timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        void Click(Vector2 at, bool twice = false, MouseButton button = MouseButton.Left) { Input.WarpMouse(at); InventoryClick(at, twice, button); }
        async Task Flip() { Click(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        Vector2 Target(string region, string owner = "avatar-backpack")
        {
            var bounds = proof.Art.Bounds(surface.Size);
            for (var y = .24f; y < .51f; y += .003f)
            for (var x = .24f; x < .82f; x += .003f)
            {
                var global = (surface.GlobalPosition + bounds.Position + new Vector2(x, y) * bounds.Size).Round();
                var at = global - surface.GlobalPosition;
                if (new[] { Vector2.Zero, Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down }.All(offset =>
                    proof.Art.StorageTarget(sim, proof.Side, surface.Size, at + offset) is { Pocket: not null } p && p.Owner == owner && p.Region == region)) return global;
            }
            throw new InvalidOperationException("Pack pocket: missing " + region + " on " + proof.Side);
        }
        Button[] Menu() => panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().ToArray();
        await Flip();
        foreach (var region in new[] { "main", "side", "front" })
        {
            if (sim.Inventory.IsOpen("avatar-backpack", region)) await Act(new(InventoryActionKind.Close, "avatar-backpack", RegionId: region));
            var target = Target(region); var before = sim.Save();
            // A native pointer motion dismisses a tooltip left up during paid closure work.
            Input.WarpMouse(target);
            using (var motion = new InputEventMouseMotion { Position = target, GlobalPosition = target }) GetViewport().PushInput(motion);
            await InventoryFrames(4);
            Check(surface.HoveredPocket is { Owner: "avatar-backpack" } p && p.Region == region && sim.Save() == before, "hover mismatch: expected " + region + ", actual " + surface.HoveredPocket?.Region + ", state changed=" + (sim.Save() != before));
            await InventoryCapture("-pack-pocket-" + region);
            Click(target, button: MouseButton.Right); await InventoryFrames();
            var verb = region == "main" ? "Unzip" : "Unbuckle";
            Check(Menu().Any(b => b.Text == verb) && !Menu().Any(b => b.Text == (region == "main" ? "Unbuckle" : "Unzip")), "menu selected wrong fastening");
            Click(new(600, 55)); await InventoryFrames();
            Click(target, true); await InventoryFrames();
            Check(sim.Save() == before && !sim.Inventory.IsOpen("avatar-backpack", region), "paused opening changed closure state");
            sim.ManualPause = false; Click(target, true);
            Check(sim.Inventory.Pending?.Request is { Kind: InventoryActionKind.Open, RegionId: var r } && r == region
                && !sim.Inventory.IsOpen("avatar-backpack", region), "opening bypassed paid work");
            var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
            clone.Restore(pending); Check(clone.Save() == pending, "paid closure did not restore exactly"); await Finish();
            Check(panel.Browser.CurrentContents == ("avatar-backpack", region), "opened wrong contents trail");
        }
        await Act(sim.GroundDropRequest("spare-dressing"));
        Vector2 Source()
        {
            var item = sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing");
            var grid = panel.FindGrid(item.Placement.ParentId, item.Placement.RegionId);
            return grid.GlobalPosition + new Vector2(1 + item.Placement.X * grid.CellPixels + 8, 44 + item.Placement.Y * grid.CellPixels + 8);
        }
        await Act(new(InventoryActionKind.Close, "avatar-backpack", RegionId: "side"));
        var beforeDrop = sim.Save();
        await InventoryDrag(Source(), Target("side"), async () =>
        { Check(surface.StorageTarget?.Region == "side" && surface.StoragePreview?.Allowed == false && surface.DropPreview is null, "closed side fell through to main or fitting"); await InventoryFrames(); });
        Check(sim.Save() == beforeDrop, "closed side release changed state");
        await Act(new(InventoryActionKind.Open, "avatar-backpack", RegionId: "side"));
        await InventoryDrag(Source(), Target("side"), async () =>
        {
            Check(surface.StorageTarget is { Owner: "avatar-backpack", Region: "side", Pocket: not null } && surface.StoragePreview?.Allowed == true, "drop missed side pocket");
            await InventoryCapture("-pack-pocket-side-drop"); sim.ManualPause = false;
        });
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.AutoPlace
            && sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement.ParentId.StartsWith("ground:", StringComparison.Ordinal), "drop bypassed paid source retention");
        await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement is { ParentId: "avatar-backpack", RegionId: "side" }, "drop went to main instead of side");
        await Flip();
        var boundsFront = proof.Art.Bounds(surface.Size);
        Check(proof.Art.StorageTarget(sim, "front", surface.Size, boundsFront.Position + new Vector2(.28f, .3f) * boundsFront.Size)?.Owner != "avatar-backpack", "harness became a container target");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "front")));
        foreach (var region in new[] { "main", "side", "front" })
        { Click(Target(region), true); await InventoryFrames(); Check(panel.Browser.CurrentContents == ("avatar-backpack", region), "front mount lost compartment identity"); }
        Input.WarpMouse(Target("main")); await InventoryFrames(); await InventoryCapture("-pack-pocket-front-main");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "back")));
        await Act(new(InventoryActionKind.Move, "spare-backpack", new("person:avatar", "front")));
        Click(Target("side", "spare-backpack"), button: MouseButton.Right); await InventoryFrames();
        Check(panel.SelectedItem == "spare-backpack", "two packs confused compartment owner");
        Click(new(600, 55)); await InventoryFrames();
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        GD.Print("PAPERDOLL_PACK_POCKET_SMOKE_PASS: main/side/front contours, independent zipper/buckle menus, paused/paid opening and restore, side refusal/paid storage, harness exclusion, both mounting sides and two pack identities.");
    }
}
