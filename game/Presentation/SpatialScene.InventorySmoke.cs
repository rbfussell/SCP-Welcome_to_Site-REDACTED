using Godot;
using SiteSimulation;
using System.IO;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class SpatialScene
{
    private async Task InventoryFrames(int count = 2)
    { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void InventoryKey(Key key)
    {
        using var press = new InputEventKey { Keycode = key, Pressed = true };
        using var release = new InputEventKey { Keycode = key, Pressed = false };
        GetViewport().PushInput(press); GetViewport().PushInput(release);
    }
    private void InventoryClick(Vector2 position, bool doubleClick = false)
    {
        using var motion = new InputEventMouseMotion { Position = position, GlobalPosition = position };
        using var press = new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = doubleClick };
        using var release = new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = false };
        GetViewport().PushInput(motion); GetViewport().PushInput(press); GetViewport().PushInput(release);
    }
    private static void InventoryMouse(InputEvent e) { using (e) Input.ParseInputEvent(e); }
    private async Task InventoryDrag(Vector2 source, Vector2 destination)
    {
        Input.WarpMouse(source); await InventoryFrames();
        InventoryMouse(new InputEventMouseMotion { Position = source, GlobalPosition = source });
        InventoryMouse(new InputEventMouseButton { Position = source, GlobalPosition = source, ButtonIndex = MouseButton.Left, Pressed = true });
        Input.WarpMouse(source + new Vector2(16, 0));
        InventoryMouse(new InputEventMouseMotion { Position = source + new Vector2(16, 0), GlobalPosition = source + new Vector2(16, 0), Relative = new(16, 0), ButtonMask = MouseButtonMask.Left });
        Input.FlushBufferedEvents(); await InventoryFrames();
        Input.WarpMouse(destination);
        InventoryMouse(new InputEventMouseMotion { Position = destination, GlobalPosition = destination, Relative = destination - source - new Vector2(16, 0), ButtonMask = MouseButtonMask.Left });
        Input.FlushBufferedEvents(); await InventoryFrames();
        InventoryMouse(new InputEventMouseButton { Position = destination, GlobalPosition = destination, ButtonIndex = MouseButton.Left, Pressed = false });
        Input.FlushBufferedEvents(); await InventoryFrames();
    }
    private async Task InventoryCapture(string suffix)
    {
        _hud.Inventory.Refresh(); await InventoryFrames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var path = Path.Combine(Path.GetDirectoryName(_capturePath!)!, Path.GetFileNameWithoutExtension(_capturePath!) + suffix + ".png");
        using var texture = GetViewport().GetTexture();
        using var image = texture.GetImage();
        if (image.SavePng(path) != Error.Ok) throw new IOException("Inventory capture failed.");
    }
    private async void RunInventorySmoke()
    {
        try
        {
            var sim = _simulation;
            InventoryKey(Key.I); await InventoryFrames();
            if (!_hud.Inventory.IsOpen || sim.ManualPause) throw new InvalidOperationException("I must open personal inventory without an automatic pause.");
            var local = sim.LocalMs; await InventoryFrames(30);
            if (sim.LocalMs <= local) throw new InvalidOperationException("Inventory inspection stopped the realtime world.");
            sim.ManualPause = true;
            var unchanged = sim.Save();
            InventoryKey(Key.O); InventoryKey(Key.R);
            if (sim.Save() != unchanged) throw new InvalidOperationException("Modal input or preview rotation leaked into gameplay state.");
            InventoryKey(Key.Escape);
            if (_hud.Inventory.IsOpen || !sim.ManualPause) throw new InvalidOperationException("Closing inventory changed the player's explicit pause.");
            sim.ManualPause = false; InventoryKey(Key.I); await InventoryFrames();
            var side = _hud.Inventory.FindGrid("avatar-backpack", "side");
            InventoryClick(side.GlobalPosition + new Vector2(15, 60));
            if (_hud.Inventory.SelectedItem != "flashlight") throw new InvalidOperationException("Actual object click did not select the flashlight.");
            InventoryKey(Key.R);
            var pocket = _hud.Inventory.FindGrid("avatar-trousers", "left");
            InventoryClick(pocket.GlobalPosition + new Vector2(15, 90));
            if (sim.Inventory.Pending?.Request.ItemId != "flashlight" || sim.Inventory.DiagnosticItems.Single(i => i.Id == "flashlight").Placement.ParentId != "avatar-backpack")
                throw new InvalidOperationException("Placement must begin timed handling, retaining the actual source.");
            // Closing the UI must not cancel or complete work; it continues under the shared scheduler.
            InventoryKey(Key.I);
            for (var i = 0; i < 240 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            var torch = sim.Inventory.DiagnosticItems.Single(i => i.Id == "flashlight");
            if (sim.Inventory.Pending is not null || torch.Placement.ParentId != "avatar-trousers" || !torch.Placement.Rotated)
                throw new InvalidOperationException("Realtime rotated handling did not complete through the common item service.");
            InventoryKey(Key.I); await InventoryFrames();
            await InventoryDrag(pocket.GlobalPosition + new Vector2(15, 59), side.GlobalPosition + new Vector2(15, 59));
            if (sim.Inventory.Pending?.Request.ItemId != "gum-packet") throw new InvalidOperationException("Native object drag/drop did not begin handling.");
            for (var i = 0; i < 240 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            if (sim.Inventory.DiagnosticItems.Single(i => i.Id == "gum-packet").Placement.ParentId != "avatar-backpack")
                throw new InvalidOperationException("Native drag/drop did not retain the item in its new physical parent.");
            InventoryClick(side.GlobalPosition + new Vector2(15, 59));
            InventoryClick(pocket.GlobalPosition + new Vector2(15, 59));
            for (var i = 0; i < 240 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            // Inspect the packet via a real double click. The six objects occupy a finer physical grid.
            InventoryClick(pocket.GlobalPosition + new Vector2(15, 59), true); await InventoryFrames();
            if (sim.InventoryContents("gum-packet", "pieces").Count != 6) throw new InvalidOperationException("Packet identity or contents changed while browsing.");
            await InventoryCapture(""); sim.ManualPause = true;
            var held = sim.Save(); _hud.Inventory.Refresh(); await InventoryFrames();
            if (sim.Save() != held) throw new InvalidOperationException("Inventory rendering mutated authoritative state.");
            var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
            clone.Restore(held);
            if (clone.Save() != held) throw new InvalidOperationException("Rendered item and employment state failed to restore.");
            _hud.Inventory.Inspect("medical-case"); await InventoryCapture("-case");
            sim.Player.Cell = new(13, 6);
            if (!sim.InventoryRoots.Any(r => r.PropId == "crate-a")) throw new InvalidOperationException("Nearby crate not available at actual physical reach.");
            _hud.Inventory.Inspect("fixture:crate-a"); await InventoryCapture("-crate");
            sim.Restore(held); _runtime.ResetAccumulator(); _hud.Inventory.Inspect("medical-case");
            _hud.Inventory.Close();
            sim.Player.Facing = 1;
            sim.ManualPause = false;
            if (!sim.ArmTraining() || !sim.Combat.Active) throw new InvalidOperationException("Could not enter inventory combat fixture.");
            // Initiative is deterministic; let AI participants act until the director's turn.
            for (var i = 0; i < 3000 && sim.Combat.CurrentActorId != sim.Player.Id; i++) sim.Tick();
            if (sim.Combat.CurrentActorId != sim.Player.Id) throw new InvalidOperationException("No director turn for inventory action.");
            InventoryKey(Key.I); await InventoryFrames();
            var request = new InventoryRequest(InventoryActionKind.Move, "flashlight", new("person:avatar", "hand"));
            var quote = sim.QuoteInventory(request); var ap = sim.Combat.For(sim.Player.Id)!.Ap;
            if (!sim.HandleInventory(request) || sim.Combat.For(sim.Player.Id)!.Ap != ap - quote.Ap) throw new InvalidOperationException("Combat packing failed to charge the pocket's AP cost.");
            for (var i = 0; i < 240 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            if (sim.Inventory.Pending is not null || sim.Inventory.DiagnosticItems.Single(i => i.Id == "flashlight").Placement.ParentId != "person:avatar")
                throw new InvalidOperationException("Paid inventory action failed to resolve.");
            sim.ManualPause = true; await InventoryCapture("-combat");
            GD.Print("INVENTORY_SMOKE_PASS: actual I/Escape, realtime inspection, input isolation, native drag/drop and click/rotate/place, timed handling continuing after view close, nested object sprites, read-only rendering, save/restore and paid combat handling.");
            _hud.Inventory.Close(); await InventoryFrames(3);
            _inventorySmokeQuit = true; // Leave the async capture/input continuation before native teardown.
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
