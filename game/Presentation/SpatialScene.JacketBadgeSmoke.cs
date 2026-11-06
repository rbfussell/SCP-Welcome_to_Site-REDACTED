using Godot;
using SiteSimulation;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task RunJacketBadgeSmoke()
    {
        var sim = _simulation; var panel = _hud.Inventory; var proof = panel.PaperdollProof; var surface = proof.Surface; var original = sim.Save();
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Jacket badge UI: " + reason); }
        async Task Finish()
        {
            var deadline = Time.GetTicksMsec() + (ulong)(sim.Inventory.Pending?.RemainingMs ?? 0) + 5000;
            while ((sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0) && Time.GetTicksMsec() < deadline) await InventoryFrames(1);
            Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "handling timed out"); sim.ManualPause = true; await InventoryFrames();
        }
        async Task Act(InventoryRequest request) { sim.ManualPause = false; Check(sim.HandleInventory(request), sim.LastMessage); await Finish(); }
        void Click(Vector2 at, bool twice = false, MouseButton button = MouseButton.Left) { Input.WarpMouse(at); InventoryClick(at, twice, button); }
        InventoryGearSurface Gear() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
        Vector2 Source(string id)
        {
            var p = sim.Inventory.DiagnosticItems.Single(i => i.Id == id).Placement; var g = panel.FindGrid(p.ParentId, p.RegionId);
            return g.GlobalPosition + new Vector2(1 + p.X * g.CellPixels + 8, 44 + p.Y * g.CellPixels + 8);
        }
        sim.Player.Cell = new(13, 6); sim.ManualPause = true;
        if (!proof.Visible) { Click(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames(); }
        panel.Inspect("avatar-coat"); await InventoryFrames();
        foreach (var region in new[] { "chest", "chest-left", "lower-left", "lower-right" })
        {
            var at = Gear().HotspotPosition(region); Input.WarpMouse(at);
            using (var motion = new InputEventMouseMotion { Position = at, GlobalPosition = at }) GetViewport().PushInput(motion);
            await InventoryFrames(4); Check(Gear().HoveredRegion == region, "mounted provider lost pocket contour");
            Click(at, button: MouseButton.Right); await InventoryFrames();
            var buttons = panel.GetChildren().OfType<PanelContainer>().Last().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>();
            Check(buttons.Any(b => b.Text == (sim.Inventory.IsOpen("avatar-coat", region) ? "Button" : "Unbutton")), "pocket context fell through to jacket zipper");
            Click(new(600, 55)); await InventoryFrames();
            if (!sim.Inventory.IsOpen("avatar-coat", region)) { sim.ManualPause = false; Click(at, true); await Finish(); }
            else { Click(at, true); await InventoryFrames(); }
            Check(panel.Browser.CurrentContents == ("avatar-coat", region), "enlarged pocket double-click failed");
        }
        await Act(sim.GroundDropRequest("spare-dressing"));
        await InventoryDrag(Source("spare-dressing"), Gear().HotspotPosition("chest-left"), async () =>
        { Check(Gear().MountPreview is null, "ordinary pocket drop became a mount"); await InventoryCapture("-jacket-pocket-highlight"); sim.ManualPause = false; });
        Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.AutoPlace, "pocket drop bypassed paid storage"); await Finish();
        Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "spare-dressing").Placement is { ParentId: "avatar-coat", RegionId: "chest-left" }, "pocket drop went elsewhere");
        await Act(sim.GroundDropRequest("personnel-badge"));
        var mount = proof.Art.MountTargets(sim, "front").Single(t => t.Owner == "avatar-coat" && t.Region == "badge-clip");
        var b = proof.Art.Bounds(surface.Size); var clip = surface.GlobalPosition + b.Position + mount.Origin * b.Size;
        Check(surface.Pick(clip - surface.GlobalPosition + new Vector2(0, 7)) != "personnel-badge", "painted badge became an item before mounting");
        await InventoryCapture("-jacket-no-badge");
        await InventoryDrag(Source("personnel-badge"), clip, async () =>
        { Check(surface.DropPreview is { Quote.Allowed: true } p && p.Request.Destination?.ParentId == "avatar-coat" && p.Request.Destination.RegionId == "badge-clip", "body clip drop failed"); await InventoryCapture("-jacket-badge-body-preview"); sim.ManualPause = false; });
        Check(sim.Inventory.Pending is not null && sim.Inventory.DiagnosticItems.Single(i => i.Id == "personnel-badge").Placement.ParentId.StartsWith("ground:", StringComparison.Ordinal), "mount bypassed source retention");
        var pending = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json")); clone.Restore(pending); Check(clone.Save() == pending, "paid body mount continuation differs"); await Finish();
        Check(surface.Pick(clip - surface.GlobalPosition + new Vector2(0, 7)) == "personnel-badge", "badge lacks own sprite identity"); await InventoryCapture("-jacket-badge-open");
        await Act(new(InventoryActionKind.CloseGarment, "avatar-coat"));
        Check(surface.Pick(clip - surface.GlobalPosition + new Vector2(0, 7)) == "personnel-badge", "zipping jacket hid its exterior badge"); await InventoryCapture("-jacket-badge-closed");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "front")));
        Check(!sim.InventoryMoveAccess("personnel-badge").Allowed && surface.Pick(clip - surface.GlobalPosition + new Vector2(0, 7)) != "personnel-badge", "front pack failed to cover real badge");
        await InventoryCapture("-jacket-badge-covered");
        await Act(new(InventoryActionKind.Move, "avatar-backpack", new("person:avatar", "back")));
        await Act(sim.GroundDropRequest("personnel-badge"));
        panel.Inspect("avatar-coat"); await InventoryFrames(); var gearClip = Gear().MountPosition(new(0, 2));
        await InventoryDrag(Source("personnel-badge"), gearClip, async () =>
        { Check(Gear().MountPreview?.Handling.Allowed == true, "enlarged clip failed mounting"); await InventoryCapture("-jacket-gear-mount-preview"); sim.ManualPause = false; }); await Finish();
        Check(Gear().MountedAt(gearClip + new Vector2(0, 20)) == "personnel-badge", "enlarged badge picking lost identity"); await InventoryCapture("-jacket-gear-badge");
        Click(Gear().SourcePosition(new(.65f, .57f)), true); await InventoryFrames();
        Check(panel.Browser.CurrentContents == ("avatar-coat", "chest-left"), "badge hid the entire pocket rather than only its material");
        var beforeBlocked = sim.Save();
        await InventoryDrag(Source("spare-dressing"), gearClip + new Vector2(0, 20), async () =>
        { Check(Gear().MountPreview is null && Gear().MountedAt(gearClip + new Vector2(0, 20)) == "personnel-badge", "badge-covered pocket became a mount or lost its exterior identity"); await InventoryFrames(); });
        Check(sim.Save() == beforeBlocked && sim.Inventory.Pending is null, "drop through opaque badge rearranged or moved pocket contents");
        panel.Close(); sim.Player.Cell = new(15, 2); sim.ManualPause = false;
        sim.Face(sim.Map.Definition.Walls.Single(w => w.Id == "door-7-5-u").Center); RefreshCamera();
        Check(sim.RequestMove(new(14, 2)), sim.LastMessage); await InventoryFrames(4); sim.ManualPause = true;
        Check(sim.Map.OpenDoors.Contains("door-7-5-u") && sim.Inventory.Pending is null, "jacket badge did not authorize automatic reader entry");
        await InventoryCapture("-jacket-reader-entry");
        sim.Restore(original); _runtime.ResetAccumulator(); RefreshCamera(); panel.Close(); panel.Open(); await InventoryFrames();
        GD.Print("JACKET_BADGE_SMOKE_PASS: four enlarged pockets, contour highlight/storage, empty jacket, real body/gear clip drops, distinct badge picking, open/closed jacket, covering, paid continuation and reader entry.");
    }
}
