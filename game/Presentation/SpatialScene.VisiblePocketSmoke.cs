using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunVisiblePocketSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof;
        var surface = proof.Surface; var original = sim.Save();
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Visible pockets: " + message); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while ((sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0) && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "paid work timed out");
            sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        Vector2 Target(string owner, string region)
        {
            var b = proof.Art.Bounds(surface.Size);
            for (var y = .2f; y < .82f; y += .003f)
            for (var x = .17f; x < .84f; x += .003f)
            {
                var at = (surface.GlobalPosition + b.Position + new Vector2(x, y) * b.Size).Round();
                if (new[] { Vector2.Zero, Vector2.Left, Vector2.Right, Vector2.Up, Vector2.Down }.All(offset =>
                    proof.Art.StorageTarget(sim, proof.Side, surface.Size, at - surface.GlobalPosition + offset) is { Pocket: not null } p && p.Owner == owner && p.Region == region)) return at;
            }
            throw new InvalidOperationException("Visible pockets: missing " + owner + "/" + region + " on " + proof.Side);
        }
        async Task Inspect(string owner, string region)
        {
            var target = Target(owner, region); Input.WarpMouse(target);
            using (var motion = new InputEventMouseMotion { Position = target, GlobalPosition = target }) GetViewport().PushInput(motion);
            await InventoryFrames(4);
            Check(surface.HoveredPocket?.Region == region, "hover addresses exact contour");
            sim.ManualPause = false; InventoryClick(target, true); await Finish();
            Check(panel.Browser.CurrentContents == (owner, region) && sim.Inventory.IsOpen(owner, region), "double click opens real compartment");
            await InventoryCapture("-visible-" + owner + "-" + region);
        }
        await Act(sim.GroundDropRequest("avatar-backpack"));
        foreach (var region in new[] { "chest", "chest-left", "lower-left", "lower-right" }) await Inspect("avatar-coat", region);
        await Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        foreach (var region in new[] { "chest", "chest-left", "lower-left", "lower-right" }) Target("avatar-coat", region);
        await Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        await Act(sim.GroundDropRequest("avatar-coat"));
        InventoryClick(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames();
        foreach (var region in new[] { "rear-left", "rear-right", "cargo-left", "cargo-right" }) await Inspect("avatar-trousers", region);
        Check(sim.Inventory.Definition("avatar-trousers").Regions!.Length == 6, "front/back cargo views share storage");
        sim.Restore(original); _runtime.ResetAccumulator(); panel.Inspect("avatar-shirt"); await InventoryFrames();
        if (proof.Side != "front") { InventoryClick(proof.Flip.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        GD.Print("VISIBLE_POCKET_SMOKE_PASS: four coat contours in both states, rear trouser pockets, shared front/back cargo and paid input.");
    }
}
