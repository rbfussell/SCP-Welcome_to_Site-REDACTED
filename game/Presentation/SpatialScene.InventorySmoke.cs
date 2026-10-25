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
    private void InventoryClick(Vector2 position, bool doubleClick = false, MouseButton button = MouseButton.Left)
    {
        using var motion = new InputEventMouseMotion { Position = position, GlobalPosition = position };
        using var press = new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = button, Pressed = true, DoubleClick = doubleClick };
        using var release = new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = button, Pressed = false };
        GetViewport().PushInput(motion); GetViewport().PushInput(press); GetViewport().PushInput(release);
    }
    private static void InventoryMouse(InputEvent e) { using (e) Input.ParseInputEvent(e); }
    private async Task InventoryDragHover(Vector2 destination)
    {
        var previous = GetViewport().GetMousePosition();
        Input.WarpMouse(destination);
        InventoryMouse(new InputEventMouseMotion { Position = destination, GlobalPosition = destination, Relative = destination - previous, ButtonMask = MouseButtonMask.Left });
        Input.FlushBufferedEvents(); await InventoryFrames();
    }
    private async Task InventoryDrag(Vector2 source, Vector2 destination, Func<Task>? preview = null)
    {
        Input.WarpMouse(source); await InventoryFrames();
        InventoryMouse(new InputEventMouseMotion { Position = source, GlobalPosition = source });
        InventoryMouse(new InputEventMouseButton { Position = source, GlobalPosition = source, ButtonIndex = MouseButton.Left, Pressed = true });
        Input.WarpMouse(source + new Vector2(16, 0));
        InventoryMouse(new InputEventMouseMotion { Position = source + new Vector2(16, 0), GlobalPosition = source + new Vector2(16, 0), Relative = new(16, 0), ButtonMask = MouseButtonMask.Left });
        Input.FlushBufferedEvents(); await InventoryFrames();
        await InventoryDragHover(destination);
        if (preview is not null) await preview();
        destination = GetViewport().GetMousePosition();
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
            if (_hud.Inventory.InspectedContainers.Count != 0) throw new InvalidOperationException("Uninspected pockets or bags were displayed at startup.");
            _hud.Inventory.Inspect("medical-case");
            if (_hud.Inventory.InspectedContainers.Count != 0) throw new InvalidOperationException("A packed case bypassed inspection of its parent.");
            await InventoryCapture("-paperdoll");
            var local = sim.LocalMs; await InventoryFrames(30);
            if (sim.LocalMs <= local) throw new InvalidOperationException("Inventory inspection stopped the realtime world.");
            sim.ManualPause = true;
            var unchanged = sim.Save();
            InventoryKey(Key.O); InventoryKey(Key.R);
            if (sim.Save() != unchanged) throw new InvalidOperationException("Modal input or preview rotation leaked into gameplay state.");
            InventoryKey(Key.Escape);
            if (_hud.Inventory.IsOpen || !sim.ManualPause) throw new InvalidOperationException("Closing inventory changed the player's explicit pause.");
            sim.ManualPause = false; InventoryKey(Key.I); await InventoryFrames();
            InventoryGrid side = null!, pocket = null!;
            async Task InspectWornStorage()
            {
                var trousers = _hud.Inventory.FindGrid("person:avatar", "legs");
                InventoryClick(trousers.GlobalPosition + new Vector2(30, 80), button: MouseButton.Right);
                var backpack = _hud.Inventory.FindGrid("person:avatar", "back");
                InventoryClick(backpack.GlobalPosition + new Vector2(30, 80), button: MouseButton.Right);
                await InventoryFrames();
                side = _hud.Inventory.FindGrid("avatar-backpack", "side");
                pocket = _hud.Inventory.FindGrid("avatar-trousers", "left");
                if (!_hud.Inventory.InspectedContainers.SequenceEqual(new[] { "avatar-trousers", "avatar-backpack" }))
                    throw new InvalidOperationException("Worn storage did not open through actual right-clicks.");
            }
            await InspectWornStorage();
            InventoryClick(side.GlobalPosition + new Vector2(15, 60));
            if (_hud.Inventory.SelectedItem != "flashlight") throw new InvalidOperationException("Actual object click did not select the flashlight.");
            InventoryKey(Key.R);
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
            await InspectWornStorage();
            sim.ManualPause = true;
            var beforePreview = sim.Save();
            var drop = side.GlobalPosition + new Vector2(15, 59);
            await InventoryDrag(pocket.GlobalPosition + new Vector2(55, 67), drop, async () =>
            {
                var panel = _hud.Inventory;
                void Preview(InventoryGrid grid, Rect2 expected, bool allowed)
                {
                    if (panel.DropPreview is not { } p || p.Grid != grid || p.Bounds != expected || p.Quote.Allowed != allowed)
                        throw new InvalidOperationException("Native drag sprite/highlight did not match the expected physical drop cells.");
                }
                Preview(side, new(1, 44, 60, 30), true);
                // Grabbing the far side of an object and moving within one cell must not offset its landing sprite.
                await InventoryDragHover(side.GlobalPosition + new Vector2(29, 73));
                Preview(side, new(1, 44, 60, 30), true);
                await InventoryCapture("-drag-valid");
                await InventoryDragHover(pocket.GlobalPosition + new Vector2(15, 90));
                Preview(pocket, new(1, 74, 60, 30), false); // Occupied by the flashlight.
                await InventoryCapture("-drag-blocked");
                InventoryKey(Key.R);
                await InventoryDragHover(side.GlobalPosition + new Vector2(15, 209));
                Preview(side, new(1, 194, 30, 60), false); // Rotated footprint extends below the pocket.
                await InventoryCapture("-drag-edge");
                InventoryKey(Key.R);
                var main = panel.FindGrid("avatar-backpack", "main");
                await InventoryDragHover(main.GlobalPosition + new Vector2(76, 144));
                Preview(main, new(59, 131, 58, 29), true); // Different rendered cell size.
                panel.Inspect("gum-packet"); await InventoryFrames();
                var fine = panel.FindGrid("gum-packet", "pieces");
                await InventoryDragHover(fine.GlobalPosition + new Vector2(15, 59));
                Preview(fine, new(1, 44, 440, 220), false); // Fine physical scale and a forbidden self-container.
                await InventoryDragHover(side.GlobalPosition + new Vector2(15, 20));
                if (panel.DropPreview is not null) throw new InvalidOperationException("A compartment heading retained a drop highlight.");
                await InventoryDragHover(side.GlobalPosition + new Vector2(15, 59));
                Preview(side, new(1, 44, 60, 30), true);
                if (sim.Save() != beforePreview) throw new InvalidOperationException("Dragging or rotating a preview mutated the simulation.");
                sim.ManualPause = false;
            });
            if (sim.Inventory.Pending?.Request.ItemId != "gum-packet") throw new InvalidOperationException("Native object drag/drop did not begin handling.");
            if (sim.Inventory.Pending.Request.Destination != new ItemPlacement("avatar-backpack", "side", 0, 0)
                || _hud.Inventory.DropPreview is not null || _hud.Inventory.DragItemId is not null)
                throw new InvalidOperationException("Released placement differed from its preview or left a stale ghost.");
            for (var i = 0; i < 240 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            if (sim.Inventory.DiagnosticItems.Single(i => i.Id == "gum-packet").Placement.ParentId != "avatar-backpack")
                throw new InvalidOperationException("Native drag/drop did not retain the item in its new physical parent.");
            // An invalid release and cancelling the modal view both leave the physical item untouched.
            await InventoryDrag(side.GlobalPosition + new Vector2(15, 59), pocket.GlobalPosition + new Vector2(15, 90));
            if (sim.Inventory.Pending is not null || _hud.Inventory.DropPreview is not null)
                throw new InvalidOperationException("An invalid release started handling or retained its preview.");
            await InventoryDrag(side.GlobalPosition + new Vector2(15, 59), pocket.GlobalPosition + new Vector2(15, 59), async () =>
            {
                InventoryKey(Key.Escape); await InventoryFrames();
                if (GetViewport().GuiIsDragging() || _hud.Inventory.DropPreview is not null)
                    throw new InvalidOperationException("Closing the inventory did not cancel its drag.");
            });
            if (sim.Inventory.Pending is not null) throw new InvalidOperationException("Closing during a drag committed a transfer.");
            InventoryKey(Key.I); await InventoryFrames();
            await InspectWornStorage();
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
            var mainGrid = _hud.Inventory.FindGrid("avatar-backpack", "main");
            InventoryClick(mainGrid.GlobalPosition + new Vector2(20, 65), button: MouseButton.Right); await InventoryFrames();
            if (!_hud.Inventory.InspectedContainers.Contains("medical-case")
                || !_hud.Inventory.InspectedContainers.Contains("avatar-backpack")) throw new InvalidOperationException("Nested inspection replaced its parent storage.");
            await InventoryCapture("-case");
            InventoryKey(Key.Escape); await InventoryFrames();
            if (!_hud.Inventory.IsOpen || _hud.Inventory.InspectedContainers.Contains("medical-case")
                || !_hud.Inventory.InspectedContainers.Contains("avatar-backpack")) throw new InvalidOperationException("Back did not close the nested view and preserve its parent/paperdoll.");
            _hud.Inventory.Inspect("medical-case");
            sim.ManualPause = false;
            _hud.Inventory.ChangeLid("avatar-backpack", "main", false);
            if (sim.Inventory.Pending is null) throw new InvalidOperationException("Physical lid closure did not begin timed handling.");
            for (var i = 0; i < 240 && sim.Inventory.Pending is not null; i++) await InventoryFrames(1);
            sim.ManualPause = true; await InventoryFrames();
            if (_hud.Inventory.InspectedContainers.Contains("medical-case") || sim.InventoryContents("avatar-backpack", "main").Count != 0
                || !_hud.Inventory.InspectedContainers.Contains("avatar-backpack")) throw new InvalidOperationException("Closing a physical lid failed to hide descendant views while preserving the bag itself.");
            _hud.Inventory.Inspect("medical-case");
            if (_hud.Inventory.InspectedContainers.Contains("medical-case")) throw new InvalidOperationException("Closed storage exposed a nested case.");
            await InventoryCapture("-closed-pack");
            sim.Restore(held); _runtime.ResetAccumulator();
            _hud.Inventory.Inspect("gum-packet"); await InventoryFrames();
            _hud.Inventory.CloseContainerView("avatar-trousers"); await InventoryFrames();
            if (_hud.Inventory.InspectedContainers.Contains("gum-packet") || !_hud.Inventory.InspectedContainers.Contains("avatar-backpack"))
                throw new InvalidOperationException("Closing a garment view did not close only its descendant views.");
            sim.Player.Cell = new(13, 6);
            if (!sim.InventoryRoots.Any(r => r.PropId == "crate-a")) throw new InvalidOperationException("Nearby crate not available at actual physical reach.");
            _hud.Inventory.Inspect("fixture:crate-a"); await InventoryCapture("-crate");
            sim.Restore(held); _runtime.ResetAccumulator();
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
            GD.Print("INVENTORY_SMOKE_PASS: paperdoll-only startup, actual right-click worn/nested inspection, parent-preserving Back, physical-lid hiding and child rejection, descendant-view cleanup, actual I/Escape, realtime inspection, input isolation, exact native drag sprite/footprint/commit, occupied and edge rejection, rotation and different grid scales, cancellation and preview cleanup, click/rotate/place, timed handling continuing after view close, read-only rendering, save/restore and paid combat handling.");
            _hud.Inventory.Close(); await InventoryFrames(3);
            _inventorySmokeQuit = true; // Leave the async capture/input continuation before native teardown.
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
