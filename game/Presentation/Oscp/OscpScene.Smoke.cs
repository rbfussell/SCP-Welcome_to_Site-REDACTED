using Godot;
using SiteSimulation;
using System.IO;
using System.Threading.Tasks;

namespace SitePresentation;

public partial class OscpScene
{
    private async Task Frames(int count = 2)
    { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void KeyInput(Key key)
    {
        GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = true });
        GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = false });
    }
    private async Task EnterCommand(string text)
    {
        var completed = new TaskCompletionSource();
        _smokeInput = () =>
        {
            _workspace.CommandLine.Text = text; _workspace.CommandLine.GrabFocus();
            KeyInput(Key.Enter);
            completed.SetResult();
        };
        await completed.Task;
    }
    private async Task Capture(string suffix)
    {
        _workspace.Refresh();
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (_workspace.HeaderHeight > 64) throw new InvalidOperationException("OSCP header wrapped vertically and obscured the workspace.");
        var path = Path.Combine(Path.GetDirectoryName(_capture!)!, Path.GetFileNameWithoutExtension(_capture!) + suffix + ".png");
        using var image = GetViewport().GetTexture().GetImage();
        if (image.SavePng(path) != Error.Ok) throw new IOException("OSCP capture failed.");
    }
    private async void RunSmoke()
    {
        try
        {
            var sim = _runtime.Simulation;
            if (!_workspace.Open(OscpDevice.Workstation)) throw new InvalidOperationException("Standalone workstation must have a valid physical context.");
            _workspace.Open(OscpDevice.Phone);
            if (!sim.ManualPause) throw new InvalidOperationException("Opening the phone must automatically pause single-player.");
            var paused = sim.Save(); await Frames();
            if (sim.Save() != paused) throw new InvalidOperationException("Phone pause must hold all simulation clocks.");
            _workspace.Close();
            if (sim.ManualPause) throw new InvalidOperationException("Closing an automatically paused phone must resume.");
            sim.ManualPause = true; _workspace.Open(OscpDevice.Phone); _workspace.Close();
            if (!sim.ManualPause) throw new InvalidOperationException("Closing the phone must preserve a pre-existing manual pause.");
            sim.ManualPause = false; _workspace.Open(OscpDevice.Workstation);
            var local = sim.LocalMs; var world = sim.WorldMs;
            _workspace.SelectApp("team");
            await EnterCommand("order tech post 8 4");
            if (sim.Operations.OrderHistory.Last().Channel != "terminal" || ! _workspace.Receipt.Contains("submitted"))
                throw new InvalidOperationException("Actual command-line Enter must submit the validated workstation order.");
            for (var i = 0; i < 240 && !sim.OscpRecords.Any(r => r.Text.Contains("Acknowledged")); i++) await Frames(1);
            if (!sim.OscpRecords.Any(r => r.Text.Contains("Acknowledged")) || sim.LocalMs <= local || sim.WorldMs <= world)
                throw new InvalidOperationException("Open OSCP must receive real acknowledgement while simulation time continues.");
            for (var i = 0; i < 240 && !sim.OscpRecords.Any(r => r.Author.Contains("R-017")); i++) await Frames(1);
            if (!sim.OscpRecords.Any(r => r.Author.Contains("R-017"))) throw new InvalidOperationException("Regional intake must arrive through actual network/report acquisition.");
            sim.ManualPause = true;
            var saved = sim.Save();
            _workspace.SelectApp("mail"); await Capture("");
            if (sim.Save() != saved) throw new InvalidOperationException("Drawing and application navigation cannot mutate a held world.");
            _workspace.Open(OscpDevice.Phone); _workspace.SelectApp("map");
            if (sim.Save() != saved) throw new InvalidOperationException("Switching devices must retain the same simulation without new ticks or commands.");
            await Capture("-phone-map");
            _workspace.SelectApp("systems");
            if (_workspace.CurrentApp == "foundation:systems") throw new InvalidOperationException("Phone cannot open workstation-only applications.");
            var records = sim.OscpRecords.ToArray();
            sim.Operations.NetworkOnline = false;
            var offline = sim.Save();
            _workspace.Execute("order tech follow");
            if (sim.Save() != offline || !sim.OscpRecords.SequenceEqual(records))
                throw new InvalidOperationException("Offline phone must retain acquired records and reject new commands without mutation.");
            _workspace.SelectApp("mail"); await Capture("-phone-offline");
            sim.Restore(saved); _runtime.ResetAccumulator();
            _workspace.Open(OscpDevice.Phone); _workspace.SelectApp("team");
            await Frames();
            sim.ManualPause = false;
            await EnterCommand("order tech post 12 2");
            var pending = sim.Operations.OrderHistory.Last();
            if (pending.Channel != "phone" || pending.Status != OrderStatus.Pending)
                throw new InvalidOperationException($"Phone Enter must submit a separate pending intent: channel={pending.Channel}, status={pending.Status}, receipt={_workspace.Receipt}, message={sim.LastMessage}, open={_workspace.IsOpen}, focus={GetViewport().GuiGetFocusOwner()?.Name}.");
            var clone = new SpatialSimulation(Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"),
                Godot.FileAccess.GetFileAsString("res://Content/oscp-terminal-operation.json"));
            clone.Restore(sim.Save());
            if (clone.Save() != sim.Save()) throw new InvalidOperationException("Handset, acquired reports and pending command must restore exactly.");
            sim.Operations.Phone = sim.Operations.Phone! with { Powered = false };
            sim.Tick(); sim.Tick();
            if (sim.Operations.OrderHistory.Last().Status != OrderStatus.Pending)
                throw new InvalidOperationException("A lost handset link must prevent pending command delivery.");
            _workspace.Refresh();
            if (_workspace.IsOpen) throw new InvalidOperationException("Unpowered phone must close the usable session.");
            sim.Operations.Phone = sim.Operations.Phone! with { Powered = true };
            for (var i = 0; i < 20 && sim.Operations.OrderHistory.Last().Status == OrderStatus.Pending; i++) sim.Tick();
            if (sim.Operations.OrderHistory.Last().Status == OrderStatus.Pending) throw new InvalidOperationException("Restored handset link must deliver once.");
            sim.ManualPause = true;
            _workspace.Open(OscpDevice.Phone); _workspace.SelectApp("mail"); await Capture("-phone");
            _workspace.CommandLine.GrabFocus(); KeyInput(Key.Escape);
            if (_workspace.IsOpen) throw new InvalidOperationException("Escape must close the modal phone.");
            sim.ManualPause = false;
            _workspace.Open(OscpDevice.Phone); _workspace.SelectApp("team");
            var beforeDraft = sim.Save();
            await EnterCommand("order observer follow");
            if (sim.Save() != beforeDraft || _workspace.PhoneDraftCount != 1)
                throw new InvalidOperationException("Paused phone order must remain an unsubmitted local draft.");
            await Capture("-phone-draft");
            _workspace.ClearPhoneDrafts();
            if (_workspace.PhoneDraftCount != 0 || sim.Save() != beforeDraft) throw new InvalidOperationException("Discarding a prepared order cannot affect the world.");
            await EnterCommand("order observer follow");
            _workspace.CommandLine.GrabFocus(); KeyInput(Key.Escape);
            if (sim.ManualPause || sim.Operations.OrderHistory.Last().Channel != "phone" || sim.Operations.OrderHistory.Last().Task != PersonnelTask.Follow
                || sim.Operations.OrderHistory.Last().Status != OrderStatus.Pending)
                throw new InvalidOperationException("Phone close must release its pause and submit the prepared intent, without immediate acknowledgement.");
            _workspace.Open(OscpDevice.Phone); await EnterCommand("order tech follow");
            var beforeLostLink = sim.Operations.OrderHistory.Count;
            sim.Operations.NetworkOnline = false; _workspace.Close();
            if (sim.Operations.OrderHistory.Count != beforeLostLink || sim.ManualPause)
                throw new InvalidOperationException("Closing must reject a draft whose link failed and still release the phone-owned pause.");
            sim.Operations.NetworkOnline = true;
            sim.ManualPause = true;
            _workspace.Open(OscpDevice.Phone); await EnterCommand("order tech follow");
            _workspace.Close();
            if (!sim.ManualPause || sim.Operations.OrderHistory.Last().Task != PersonnelTask.Follow || sim.Operations.OrderHistory.Last().Status != OrderStatus.Pending)
                throw new InvalidOperationException("Sending a prepared intent must preserve a pre-existing manual pause and pending physical delivery.");
            GD.Print("OSCP_SMOKE_PASS: phone automatic pause/ownership, unsubmitted drafts and validated submission on close, standalone real runtime; Enter command/acknowledgement, continuing clocks, no mutation on app/device swaps, profile gating, offline cache/rejection, save/restore, handset loss/recovery, Escape, CRT/phone/map captures.");
            GetTree().Quit();
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
