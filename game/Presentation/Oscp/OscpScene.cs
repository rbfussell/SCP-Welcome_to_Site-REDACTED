using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Standalone terminal bootstrap: real site services, no spatial renderer or second tick loop.</summary>
public partial class OscpScene : Control
{
    private SimulationRuntime _runtime = null!;
    private VBoxContainer _home = null!;
    private OscpWorkspace _workspace = null!;
    private string? _capture;
    private int _frames;
    private Action? _smokeInput;
    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("SCP — OSCP / Site Operations");
        _runtime = SimulationRuntime.Load("oscp-terminal-operation.json");
        var background = new ColorRect { Color = new("111816") }; AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var reopen = new VBoxContainer { Position = new(30, 30) }; _home = reopen; AddChild(reopen);
        var title = new Label { Text = "OSCP / SITE OPERATIONS\nSame site, personnel and continuing simulation" }; reopen.AddChild(title);
        var terminal = new Button { Text = "Open workstation [T]" }; reopen.AddChild(terminal); terminal.Pressed += () => _workspace.Open(OscpDevice.Workstation);
        var phone = new Button { Text = "Open field phone [P]" }; reopen.AddChild(phone); phone.Pressed += () => _workspace.Open(OscpDevice.Phone);
        _workspace = new OscpWorkspace(); AddChild(_workspace); _workspace.Initialize(_runtime.Simulation);
        var args = OS.GetCmdlineUserArgs();
        _workspace.Open(args.Contains("--field-phone") ? OscpDevice.Phone : OscpDevice.Workstation);
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == "--oscp-smoke") _capture = args[i + 1];
    }
    public override void _Process(double delta)
    {
        _runtime.Advance(delta);
        var input = _smokeInput; _smokeInput = null; input?.Invoke();
        _workspace.Refresh(); _home.Visible = !_workspace.IsOpen;
        if (_capture is not null && ++_frames == 15) RunSmoke();
    }
    public override void _Input(InputEvent e)
    {
        if (_workspace.IsOpen && e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        { _workspace.Close(); GetViewport().SetInputAsHandled(); }
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if (_workspace.IsOpen) { _workspace.HandleInput(e); return; }
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.T) _workspace.Open(OscpDevice.Workstation);
        if (key.Keycode == Key.P) _workspace.Open(OscpDevice.Phone);
        if (key.Keycode == Key.Space) _runtime.Simulation.ManualPause = !_runtime.Simulation.ManualPause;
    }
}
