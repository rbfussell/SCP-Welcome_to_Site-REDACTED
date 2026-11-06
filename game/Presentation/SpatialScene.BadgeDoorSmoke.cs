using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunBadgeDoorSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Badge door UI: " + reason); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while (sim.Inventory.Pending is not null && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null, "paid handling timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        sim.Player.Cell = new(13, 6); sim.ManualPause = true;
        await Act(sim.GroundDropRequest("personnel-badge"));
        var badge = sim.Inventory.DiagnosticItems.Single(i => i.Id == "personnel-badge");
        var source = panel.FindGrid(badge.Placement.ParentId, badge.Placement.RegionId);
        var hand = panel.FindGrid("person:avatar", "hand");
        var from = source.GlobalPosition + new Vector2(1 + badge.Placement.X * source.CellPixels + 8, 44 + badge.Placement.Y * source.CellPixels + 8);
        await InventoryDrag(from, hand.GlobalPosition + new Vector2(30, 80), async () =>
        { await InventoryCapture("-badge-hand-drag"); sim.ManualPause = false; });
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Move, "native hand drop did not buy handling"); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "personnel-badge").Placement is { ParentId: "person:avatar", RegionId: "hand" }, "badge not equipped in hand");
        panel.Close(); sim.Player.Cell = new(15, 2);
        sim.ManualPause = false; sim.Face(sim.Map.Definition.Walls.Single(w => w.Id == "door-7-5-u").Center); sim.ManualPause = true;
        RefreshCamera(); await InventoryFrames();
        var before = sim.Save(); SmokeKey(Key.E); await InventoryFrames();
        Check(sim.Save() == before && !sim.Map.OpenDoors.Contains("door-7-5-u"), "paused E opened reader");
        sim.ManualPause = false; SmokeKey(Key.E); await InventoryFrames(1);
        Check(sim.Inventory.Pending?.Request is { Kind: InventoryActionKind.PresentBadge, TargetId: "door-7-5-u" }
            && !sim.Map.OpenDoors.Contains("door-7-5-u"), "E bypassed manual badge presentation");
        await InventoryCapture("-badge-presenting"); await Finish();
        Check(sim.Map.OpenDoors.Contains("door-7-5-u"), "paid presentation failed to open door");
        await InventoryCapture("-badge-reader-open");
        sim.ManualPause = false; SmokeKey(Key.E); await InventoryFrames(); sim.ManualPause = true;
        Check(!sim.Map.OpenDoors.Contains("door-7-5-u"), "door close failed");
        await Act(new(InventoryActionKind.Move, "personnel-badge", new("avatar-trousers", "cargo-left")));
        sim.ManualPause = false; SmokeKey(Key.E); await InventoryFrames(1); sim.ManualPause = true;
        Check(!sim.Map.OpenDoors.Contains("door-7-5-u") && sim.Inventory.Pending is null, "pocket badge authorized entry");
        await InventoryCapture("-badge-pocket-denied");
        await Act(new(InventoryActionKind.Move, "personnel-badge", new("avatar-shirt", "badge-clip", AttachmentPose: new(0, 2))));
        await Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        sim.ManualPause = false; SmokeKey(Key.E); await InventoryFrames(1); sim.ManualPause = true;
        Check(!sim.Map.OpenDoors.Contains("door-7-5-u"), "covered badge authorized entry");
        await InventoryCapture("-badge-covered-denied");
        await Act(new(InventoryActionKind.OpenGarment, "avatar-coat"));
        sim.ManualPause = false;
        Check(sim.RequestMove(new(14, 2)), sim.LastMessage); await InventoryFrames(4); sim.ManualPause = true;
        Check(sim.Map.OpenDoors.Contains("door-7-5-u") && sim.Inventory.Pending is null, "moving wearer failed automatic reader use");
        await InventoryCapture("-badge-automatic-entry");
        sim.Restore(original); _runtime.ResetAccumulator(); RefreshCamera(); if (!panel.IsOpen) InventoryKey(Key.I); await InventoryFrames();
        GD.Print("BADGE_DOOR_SMOKE_PASS: real badge-to-hand drag, E presentation with time, paused refusal, pocket/covered denial, visible worn automatic entry and reader feedback.");
    }
}
