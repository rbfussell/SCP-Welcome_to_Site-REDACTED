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
            var sim = _simulation; var panel = _hud.Inventory;
            void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
            async Task Finish()
            {
                for (var i = 0; i < 900 && (sim.Inventory.Pending is not null || sim.Inventory.SecuringRemaining != 0); i++) await InventoryFrames(1);
                Check(sim.Inventory.Pending is null && sim.Inventory.SecuringRemaining == 0, "Handling/securing failed to finish.");
            }
            Button Navigation(bool gear, string text) => panel.Browser.GetChild<InventoryContainerView>(gear ? 0 : 1)
                .GetChild<HBoxContainer>(0).GetChildren().OfType<Button>().Single(b => b.Text == text);
            void Nav(bool gear, string text) => InventoryClick(Navigation(gear, text).GetGlobalRect().GetCenter());
            InventoryGearSurface Surface() => panel.Browser.GetChild<InventoryContainerView>(0).GetChildren().OfType<InventoryGearSurface>().Single();
            async Task OpenPocket(string owner, string region)
            {
                panel.Inspect(owner); await InventoryFrames();
                InventoryClick(Surface().HotspotPosition(region), true); await InventoryFrames();
            }
            InventoryKey(Key.I); await InventoryFrames();
            Check(panel.IsOpen && !sim.ManualPause && panel.InspectedContainers.Count == 0, "Inventory startup must be paperdoll only without pausing.");
            panel.Inspect("medical-case"); Check(panel.InspectedContainers.Count == 0, "Hidden case bypassed parent inspection.");
            await InventoryCapture("-paperdoll");
            InventoryClick(panel.EngineeringSlots.GetGlobalRect().GetCenter()); await InventoryFrames();
            if (OS.GetCmdlineUserArgs().Contains("--badge-door-smoke"))
            {
                await RunBadgeDoorSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--shirt-pocket-smoke"))
            {
                sim.ManualPause = true; sim.Player.Cell = new(13, 6);
                InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
                await RunPaperdollShirtSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--pack-pocket-smoke"))
            {
                sim.ManualPause = true; sim.Player.Cell = new(13, 6);
                InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
                await RunPaperdollPackPocketSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--pocket-smoke"))
            {
                sim.ManualPause = true; sim.Player.Cell = new(13, 6);
                InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
                await RunPaperdollPocketSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--worn-storage-smoke"))
            {
                sim.ManualPause = true; sim.Player.Cell = new(13, 6);
                InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
                await RunWornStorageSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--sprite-drop-smoke"))
            {
                sim.ManualPause = true; sim.Player.Cell = new(13, 6);
                InventoryClick(panel.PaperdollProofToggle.GetGlobalRect().GetCenter()); await InventoryFrames();
                await RunPaperdollDropSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--paperdoll-smoke"))
            {
                await RunPaperdollSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--hook-smoke"))
            {
                await RunHookSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--transfer-smoke"))
            {
                await RunTransferSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--ground-smoke"))
            {
                await RunGroundSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--body-drop-smoke"))
            {
                await RunBodyDropSmoke(); await RunBodyInspectionSmoke(); panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true; return;
            }
            await RunWearSmoke();
            await RunPackingSmoke();
            await RunBodyFitSmoke();
            await RunBodyDropSmoke();
            await RunBodyInspectionSmoke();
            await RunPaperdollSmoke();
            await RunCoverageSmoke();
            await RunAttachmentSmoke();
            await RunOverlaySmoke();
            await RunHookSmoke();
            await RunTransferSmoke();
            await RunGroundSmoke();
            await RunBadgeDoorSmoke();
            var local = sim.LocalMs; await InventoryFrames(20); Check(sim.LocalMs > local, "Inspection froze realtime simulation.");
            sim.ManualPause = true; var original = sim.Save();
            await OpenPocket("avatar-backpack", "main");
            Check(panel.Browser.CurrentGear == "avatar-backpack" && panel.Browser.CurrentContents == ("avatar-backpack", "main"), "Gear/contents slots did not open independently.");
            async Task Hover(Vector2 position)
            {
                using var motion = new InputEventMouseMotion { Position = position, GlobalPosition = position };
                GetViewport().PushInput(motion); await InventoryFrames();
            }
            var surface = Surface();
            var segmentSave = sim.Save();
            await Hover(surface.HotspotPosition("side"));
            Check(surface.HasHighlight && surface.HoveredRegion == "side", "Hover did not identify the visible side pouch.");
            await InventoryCapture("-side-hover");
            // Inside the old bounding rectangle but outside the traced contour: it must not be clickable.
            var blank = surface.SourcePosition(new(0.80f, 0.49f));
            await Hover(blank); Check(!surface.HasHighlight, "A polygon bounding-box corner highlighted empty space.");
            InventoryClick(blank, true); await InventoryFrames();
            Check(panel.Browser.CurrentContents == ("avatar-backpack", "main"), "Blank-space double-click opened a compartment.");
            await Hover(surface.SourcePosition(new(0.02f, 0.02f)));
            Check(!surface.HasHighlight, "Transparent image margin retained a highlight.");
            await Hover(surface.HotspotPosition("main"));
            Check(surface.HoveredRegion == "main", "Main segment hit area drifted from artwork.");
            await InventoryCapture("-main-hover");
            // Same source pixel through a different aspect ratio, matching the real drawing transform.
            var beforeSize = surface.Size;
            surface.Size = new(330, 410); await InventoryFrames();
            await Hover(surface.HotspotPosition("side"));
            Check(surface.HoveredRegion == "side", "Segment mapping drifted when the view was resized.");
            surface.Size = beforeSize; await InventoryFrames();
            await Hover(panel.FindGrid("person:avatar", "hand").GetGlobalRect().GetCenter());
            Check(!surface.HasHighlight && sim.Save() == segmentSave, "Leaving the illustration retained a highlight or mutated inventory.");
            var main = panel.FindGrid("avatar-backpack", "main");
            InventoryClick(main.GlobalPosition + new Vector2(20, 65), true); await InventoryFrames();
            Check(panel.Browser.CurrentGear == "avatar-backpack" && panel.Browser.CurrentContents == ("medical-case", "inside"), "Nested case must replace contents, retaining gear.");
            await InventoryCapture("-case");
            var beforeEscape = sim.Save(); InventoryKey(Key.Escape); await InventoryFrames();
            Check(!panel.IsOpen && sim.Save() == beforeEscape && sim.Inventory.IsOpen("avatar-backpack", "main") && sim.Inventory.IsOpen("medical-case", "inside"), "Esc changed physical closure state.");
            InventoryKey(Key.I); await OpenPocket("avatar-trousers", "left");
            var pocket = panel.FindGrid("avatar-trousers", "left");
            InventoryClick(pocket.GlobalPosition + new Vector2(15, 59), true); await InventoryFrames();
            Check(panel.Browser.CurrentContents == ("gum-packet", "pieces") && sim.InventoryContents("gum-packet", "pieces").Count == 6, "Packet did not replace the pocket at its own fine grid scale.");
            var packetPreview = InventoryArt.Project("gum-pack", new Rect2(0, 0, 330, 350), new(0.50f, 0.49f));
            var packetSource = InventoryArt.Unproject("gum-pack", new Rect2(0, 0, 330, 350), packetPreview);
            Check(packetSource.DistanceTo(new Vector2(0.50f, 0.49f)) < 0.0001f, "Rotated item artwork failed to share its source-image transform.");
            await InventoryCapture("-packet");
            Nav(false, "↑"); await InventoryFrames();
            Check(panel.Browser.CurrentContents == ("avatar-trousers", "left") && sim.Inventory.SecuringRemaining == 1, "Contents Up must return one level and queue normal securing.");
            sim.ManualPause = false; await Finish(); sim.ManualPause = true;
            Check(!sim.Inventory.IsOpen("gum-packet", "pieces"), "Up supplied a visual-only closure.");
            // Native context menu, positioned beside the pointer, opens the chosen top-level object.
            var pack = panel.FindGrid("person:avatar", "back");
            InventoryClick(pack.GlobalPosition + new Vector2(30, 80), button: MouseButton.Right); await InventoryFrames();
            var context = panel.GetChildren().OfType<PanelContainer>().Last();
            var openButton = context.GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Single(b => b.Text == "Open");
            InventoryClick(openButton.GetGlobalRect().GetCenter()); await InventoryFrames();
            Check(panel.Browser.CurrentGear == "avatar-backpack" && panel.Browser.CurrentContents == ("avatar-trousers", "left"), "Context Open must switch gear while retaining the source pocket.");
            InventoryClick(Surface().HotspotPosition("side"), true); await InventoryFrames();
            var side = panel.FindGrid("avatar-backpack", "side"); var hand = panel.FindGrid("person:avatar", "hand");
            var dragSave = sim.Save();
            await InventoryDrag(side.GlobalPosition + new Vector2(15, 59), hand.GlobalPosition + new Vector2(20, 70), async () =>
            {
                Check(panel.DropPreview is { Quote.Allowed: true } p && p.Grid == hand && p.Bounds == new Rect2(1, 44, 90, 90), "Native drag footprint did not snap to its actual hand destination.");
                Check(sim.Save() == dragSave, "Preview mutated authoritative state."); await InventoryCapture("-drag-valid");
            });
            Check(sim.Inventory.Pending is null && sim.Save() == dragSave, "Paused release granted a free move.");
            sim.ManualPause = false;
            await InventoryDrag(side.GlobalPosition + new Vector2(15, 59), hand.GlobalPosition + new Vector2(20, 70));
            Check(sim.Inventory.Pending?.Request.ItemId == "flashlight", "Native drag did not begin paid handling.");
            InventoryKey(Key.Escape); await Finish(); Check(!panel.IsOpen, "Pending action reopened inventory.");
            InventoryKey(Key.I); sim.ManualPause = true; await OpenPocket("avatar-trousers", "left");
            pocket = panel.FindGrid("avatar-trousers", "left"); hand = panel.FindGrid("person:avatar", "hand");
            sim.ManualPause = false;
            await InventoryDrag(hand.GlobalPosition + new Vector2(30, 80), pocket.GlobalPosition + new Vector2(15, 90), async () =>
            {
                InventoryKey(Key.R); await InventoryFrames();
                Check(panel.DropPreview is { Quote.Allowed: true } p && p.Bounds == new Rect2(1, 74, 120, 30), "Rotated object preview did not match the exact destination cells.");
            });
            await Finish(); sim.ManualPause = true;
            Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "flashlight").Placement == new ItemPlacement("avatar-trousers", "left", 0, 1, true), "Rotated drop committed a different placement.");
            var blockedSave = sim.Save();
            await InventoryDrag(pocket.GlobalPosition + new Vector2(15, 59), pocket.GlobalPosition + new Vector2(15, 90), async () =>
            { Check(panel.DropPreview is { Quote.Allowed: false }, "Occupied cells were accepted."); await InventoryCapture("-drag-blocked"); });
            Check(sim.Inventory.Pending is null && sim.Save() == blockedSave, "Blocked drag mutated inventory.");
            await InventoryDrag(pocket.GlobalPosition + new Vector2(15, 59), pocket.GlobalPosition + new Vector2(105, 209), async () =>
            { Check(panel.DropPreview is { Quote.Allowed: false }, "A footprint extending past the compartment edge was accepted."); await InventoryFrames(); });
            Check(sim.Inventory.Pending is null && sim.Save() == blockedSave, "Edge-overflow release mutated inventory.");
            // A late physical opening cannot resurrect a branch after switching gear.
            panel.ChangeLid("avatar-backpack", "main", false); // paused: rejected
            panel.Inspect("avatar-backpack"); await InventoryFrames(); sim.ManualPause = false;
            InventoryClick(Surface().HotspotPosition("main"), button: MouseButton.Right); await InventoryFrames();
            var secureMenu = panel.GetChildren().OfType<PanelContainer>().Last();
            var zip = secureMenu.GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Single(b => b.Text == "Zip");
            InventoryClick(zip.GetGlobalRect().GetCenter()); await Finish();
            await OpenPocket("avatar-backpack", "main");
            Check(sim.Inventory.Pending?.Request.Kind == InventoryActionKind.Open && panel.Browser.CurrentContents == ("avatar-trousers", "left"), "Pending main opening must retain the source pocket without revealing secured contents.");
            panel.Inspect("avatar-trousers"); await Finish(); await InventoryFrames();
            Check(panel.Browser.CurrentGear == "avatar-trousers" && panel.Browser.CurrentContents == ("avatar-trousers", "left") && sim.Inventory.IsOpen("avatar-backpack", "main"), "Late opening replaced the retained source after switching gear.");
            sim.ManualPause = true; await OpenPocket("avatar-backpack", "main"); panel.Inspect("medical-case"); await InventoryFrames();
            Nav(false, "×"); await InventoryFrames();
            Check(panel.Browser.CurrentContents is null && panel.Browser.CurrentGear == "avatar-backpack" && sim.Inventory.SecuringRemaining == 2
                && sim.Inventory.IsOpen("avatar-backpack", "main") && sim.Inventory.IsOpen("medical-case", "inside"), "Contents × failed to queue child-before-parent securing without free closure.");
            var queueSave = sim.Save(); var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/scp650-operation.json"));
            clone.Restore(queueSave); Check(clone.Save() == queueSave, "Securing queue did not persist exactly.");
            InventoryKey(Key.Escape); sim.ManualPause = false; await Finish();
            Check(!sim.Inventory.IsOpen("medical-case", "inside") && !sim.Inventory.IsOpen("avatar-backpack", "main"), "Queued closure did not continue after hiding the UI.");
            // Outer garment contents retain their real owner while the middle descends to an exposed shirt.
            sim.Restore(original); _runtime.ResetAccumulator(); InventoryKey(Key.I); await OpenPocket("avatar-coat", "chest");
            var gearView = panel.Browser.GetChild<InventoryContainerView>(0);
            var under = gearView.GetChildren().OfType<VBoxContainer>().SelectMany(c => c.GetChildren().OfType<Button>()).Single(b => b.Text.Contains("work shirt", StringComparison.OrdinalIgnoreCase));
            InventoryClick(under.GetGlobalRect().GetCenter()); await InventoryFrames();
            Check(panel.Browser.CurrentGear == "avatar-shirt" && panel.Browser.CurrentContents == ("avatar-coat", "chest"), "Jacket pocket lost its owner during lower-layer inspection.");
            Nav(true, "↑"); await InventoryFrames();
            Check(panel.Browser.CurrentGear == "avatar-coat" && panel.Browser.CurrentContents == ("avatar-coat", "chest"), "Middle Up discarded a retained outer-garment pocket.");
            Check(sim.Inventory.SecuringRemaining == 0 && sim.Inventory.DiagnosticItems.Single(i => i.Id == "avatar-coat").GarmentOpen,
                "Returning from shirt inspection unexpectedly re-fastened the jacket.");
            PanelContainer Menu() => panel.GetChildren().OfType<PanelContainer>().Last();
            async Task FrontMenu()
            { InventoryClick(Surface().HotspotPosition(null), button: MouseButton.Right); await InventoryFrames(); }
            Button MenuAction(string label) => Menu().GetChild<VBoxContainer>(0).GetChildren().OfType<Button>().Single(b => b.Text == label);
            CheckButton Preference() => Menu().GetChild<VBoxContainer>(0).GetChildren().OfType<CheckButton>().Single();
            await FrontMenu();
            Check(MenuAction("Zip") is not null && !Preference().ButtonPressed, "Open jacket menu must offer Zip and default to leaving the front open.");
            // Keep the fold-out menu open while another valid command resolves; its label must read live state.
            sim.ManualPause = false;
            Check(sim.HandleInventory(new(InventoryActionKind.CloseGarment, "avatar-coat")), "Could not close jacket for state-menu check.");
            await Finish(); await InventoryFrames();
            Check(MenuAction("Unzip") is not null, "An already-open menu did not update when the zipper's physical boolean changed.");
            await InventoryCapture("-menu-unzip");
            InventoryClick(MenuAction("Unzip").GetGlobalRect().GetCenter()); await Finish(); sim.ManualPause = true;
            await OpenPocket("avatar-coat", "chest");
            InventoryClick(Surface().HotspotPosition("chest"), button: MouseButton.Right); await InventoryFrames();
            Check(Preference().ButtonPressed && MenuAction("Button") is not null, "Open buttoned-pocket menu has the wrong action or default.");
            InventoryClick(Preference().GetGlobalRect().GetCenter()); await InventoryFrames();
            Check(!InventoryClosures.SecureOnClose(sim.Inventory, "avatar-coat", "chest")
                && !InventoryClosures.SecureOnClose(sim.Inventory, "avatar-coat"), "Pocket override affected the wrong fastening.");
            var noAuto = sim.Save();
            Nav(true, "×"); await InventoryFrames();
            Check(panel.Browser.CurrentGear is null && panel.Browser.CurrentContents is null && sim.Inventory.SecuringRemaining == 0 && sim.Save() == noAuto,
                "Dismissal with both fastening preferences off changed physical state.");
            await OpenPocket("avatar-coat", "chest");
            InventoryClick(Surface().HotspotPosition("chest"), button: MouseButton.Right); await InventoryFrames();
            Check(!Preference().ButtonPressed, "Pocket preference reset when its view reopened.");
            InventoryClick(Preference().GetGlobalRect().GetCenter()); await InventoryFrames();
            Nav(true, "×"); await InventoryFrames();
            Check(sim.Inventory.SecuringRemaining == 1, "Default pocket securing should leave the separate jacket front open.");
            sim.ManualPause = false; await Finish();
            Check(sim.Inventory.DiagnosticItems.Single(i => i.Id == "avatar-coat").GarmentOpen && !sim.Inventory.IsOpen("avatar-coat", "chest"),
                "Closing the pocket also fastened the jacket front.");
            await OpenPocket("avatar-coat", "chest"); await Finish(); sim.ManualPause = true;
            await FrontMenu();
            InventoryClick(Preference().GetGlobalRect().GetCenter()); await InventoryFrames();
            Check(InventoryClosures.SecureOnClose(sim.Inventory, "avatar-coat") && !InventoryClosures.SecureOnClose(sim.Inventory, "observer-coat"),
                "Jacket preference must belong to this gear instance, not a global setting.");
            await InventoryCapture("-jacket-preference");
            Nav(true, "×"); await InventoryFrames();
            Check(sim.Inventory.SecuringRemaining == 2, "Opted-in jacket and pocket must both use normal paid securing.");
            sim.ManualPause = false; await Finish();
            Check(!sim.Inventory.DiagnosticItems.Single(i => i.Id == "avatar-coat").GarmentOpen && !sim.Inventory.IsOpen("avatar-coat", "chest"),
                "Per-piece opted-in securing failed to finish.");
            sim.Restore(original); _runtime.ResetAccumulator(); panel.Close(); panel.Open(); await OpenPocket("avatar-backpack", "main"); panel.Inspect("medical-case");
            await InventoryCapture("");
            GD.Print("INVENTORY_SMOKE_PASS: three inspection panes plus permanent ground dock, gear/contents ancestry, live Zip/Unzip and Button/Unbutton menus, per-piece securing preferences, illustrated hotspot input, parent/child Up and ×, Esc unchanged physical state, late-opening cancellation, paused closure queue, persistence, native snapped drag/rotation/blocked release, paid work continuing with UI hidden, clothing inspection and coverage.");
            panel.Close(); await InventoryFrames(3); _inventorySmokeQuit = true;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
