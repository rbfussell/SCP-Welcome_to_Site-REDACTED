using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Attachable OSCP mode view. The host alone ticks the shared simulation.</summary>
public partial class OscpWorkspace : Control
{
    private SpatialSimulation _sim = null!;
    private OscpApplications _apps = null!;
    private OscpDeviceFrame _frame = null!;
    private Control _screen = null!;
    private VBoxContainer _nav = null!, _actions = null!;
    private HBoxContainer _header = null!;
    internal float HeaderHeight => _header.Size.Y;
    private HBoxContainer _body = null!, _toolbar = null!;
    private Label _subtitle = null!;
    private Label _session = null!, _link = null!, _clock = null!, _heading = null!, _receipt = null!, _scope = null!;
    private ScrollContainer _contentScroll = null!;
    private RichTextLabel _document = null!;
    private LineEdit _command = null!;
    private Button _clearDrafts = null!;
    private OptionButton _recipient = null!;
    private OscpLocalPlan _map = null!;
    private bool _ownsPhonePause;
    private string _app = "foundation:operations";
    private readonly Dictionary<string, Button> _navigation = [];
    public OscpDevice Device { get; private set; }
    public bool IsOpen => Visible;
    public string CurrentApp => _app;
    public string Receipt => _receipt.Text;
    internal LineEdit CommandLine => _command;
    public Action? Closed { get; set; }

    public void Initialize(SpatialSimulation sim)
    {
        _sim = sim;
        _apps = new(Godot.FileAccess.GetFileAsString("res://Content/oscp-apps.json"));
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
        Theme = new Theme { DefaultFontSize = 14 };
        Theme.SetColor("font_color", "Label", new("dddcca"));
        Theme.SetColor("default_color", "RichTextLabel", new("c7d3c9"));
        Theme.SetStylebox("normal", "Button", Style("17211e", "435249"));
        Theme.SetStylebox("hover", "Button", Style("29392e", "859581"));
        Theme.SetStylebox("pressed", "Button", Style("3b2422", "aa6960"));
        var dim = new ColorRect { Color = new(0.015f, 0.025f, 0.025f, 0.92f), MouseFilter = MouseFilterEnum.Stop };
        AddChild(dim); dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _frame = new OscpDeviceFrame { MouseFilter = MouseFilterEnum.Stop }; AddChild(_frame);
        _screen = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Stop }; _frame.AddChild(_screen);
        var margin = new MarginContainer(); _screen.AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 16);
        var column = new VBoxContainer(); margin.AddChild(column); column.AddThemeConstantOverride("separation", 9);
        var header = new HBoxContainer(); header.AddThemeConstantOverride("separation", 12); _header = header; column.AddChild(header);
        var brand = Text(header, "OSCP", 28, new("db7e75")); brand.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; brand.AutowrapMode = TextServer.AutowrapMode.Off;
        _subtitle = Text(header, "INFORMATION & OPERATIONS", 12, new("a6b0a0")); _subtitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var brightness = new HSlider { MinValue = 0.7, MaxValue = 1.1, Step = 0.05, Value = 1, CustomMinimumSize = new(68, 0), TooltipText = "Screen brightness" };
        brightness.ValueChanged += value => { _screen.Modulate = new((float)value, (float)value, (float)value); _frame.Brightness = (float)value; _frame.QueueRedraw(); };
        header.AddChild(brightness);
        Button(header, "CLOSE", Close).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _session = Text(column, "", 12, new("c1b6a2"));
        _link = Text(column, "", 12, new("a8c7a4"));
        _clock = Text(column, "", 11, new("9fae9e"));
        column.AddChild(new HSeparator());
        _body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; column.AddChild(_body); _body.AddThemeConstantOverride("separation", 20);
        _nav = new VBoxContainer { CustomMinimumSize = new(164, 0) }; _body.AddChild(_nav);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _contentScroll = scroll; _body.AddChild(scroll);
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(content); content.AddThemeConstantOverride("separation", 12);
        _heading = Text(content, "", 20, new("dedcca"));
        _scope = Text(content, "", 12, new("9eaf9f"));
        _toolbar = new HBoxContainer(); content.AddChild(_toolbar);
        _recipient = new OptionButton { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill }; _toolbar.AddChild(_recipient);
        foreach (var actor in sim.Actors.Where(a => a != sim.Player)) _recipient.AddItem(actor.Name);
        _actions = new VBoxContainer(); content.AddChild(_actions);
        var row = new HBoxContainer(); _actions.AddChild(row);
        Button(row, "Observe", () => Send(PersonnelTask.Observe)); Button(row, "Follow", () => Send(PersonnelTask.Follow));
        row = new HBoxContainer(); _actions.AddChild(row);
        Button(row, "Take post", () => Send(PersonnelTask.Post)); Button(row, "Recover", () => Send(PersonnelTask.Transport));
        _clearDrafts = Button(_actions, "Clear prepared orders", ClearPhoneDrafts);
        _map = new OscpLocalPlan { Simulation = sim, CustomMinimumSize = new(200, 225), SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddChild(_map);
        _document = new RichTextLabel { BbcodeEnabled = false, FitContent = true, ScrollActive = false, CustomMinimumSize = new(170, 80), SizeFlagsHorizontal = SizeFlags.ExpandFill, SelectionEnabled = true };
        _document.AddThemeFontSizeOverride("normal_font_size", 14); content.AddChild(_document);
        column.AddChild(new HSeparator());
        _receipt = Text(column, "", 12, new("d8b77a"));
        var commandRow = new HBoxContainer(); column.AddChild(commandRow);
        var prompt = Text(commandRow, ">", 16, new("db7e75"));
        prompt.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; prompt.AutowrapMode = TextServer.AutowrapMode.Off;
        _command = new LineEdit { PlaceholderText = "status / team / records / mail / map / help", SizeFlagsHorizontal = SizeFlags.ExpandFill, MaxLength = 200, KeepEditingOnTextSubmit = true };
        commandRow.AddChild(_command); _command.TextSubmitted += Execute;
        Button(commandRow, "ENTER", () => Execute(_command.Text)).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        Text(column, "SCiPnet · Esc closes · Space pauses when not typing", 11, new("83998b"));
        Visible = false;
    }
    private static StyleBoxFlat Style(string background, string border) => new()
    {
        BgColor = new(background), BorderColor = new(border), BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
        ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 7, ContentMarginBottom = 7
    };
    private static Label Text(Node parent, string text, int size, Color color)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeColorOverride("font_color", color); label.AddThemeFontSizeOverride("font_size", size); parent.AddChild(label); return label;
    }
    private static Button Button(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.Pressed += action; parent.AddChild(button); return button;
    }
    public bool Open(OscpDevice device)
    {
        var access = _sim.OscpAccess(device);
        if (!access.Available) return false;
        if (IsOpen && Device == OscpDevice.Phone && device != OscpDevice.Phone) CompletePhoneSession(true);
        Device = device; _frame.Device = device;
        if (device == OscpDevice.Phone && !_sim.ManualPause)
        { _ownsPhonePause = true; _sim.ManualPause = true; }
        foreach (var child in _nav.GetChildren()) { _nav.RemoveChild(child); child.QueueFree(); }
        _navigation.Clear();
        foreach (var app in _apps.For(device))
        {
            var button = Button(_nav, device == OscpDevice.Phone ? app.Label : app.Symbol + "  " + app.Label, () => SelectApp(app.Id));
            button.AddThemeFontSizeOverride("font_size", device == OscpDevice.Phone ? 11 : 14);
            button.Alignment = HorizontalAlignment.Left; button.ToggleMode = true; _navigation.Add(app.Id, button);
        }
        Visible = true; _receipt.Text = "Session established · " + access.DeviceId;
        SelectApp("foundation:operations"); Refresh();
        return true;
    }
    public void Toggle(OscpDevice device) { if (Visible && Device == device) Close(); else Open(device); }
    private void ReleasePhonePause()
    {
        if (_ownsPhonePause && _sim.ManualPause) _sim.ManualPause = false;
        _ownsPhonePause = false;
    }
    public void Close() => CloseSession(true);
    private void CloseSession(bool sendDrafts) { CompletePhoneSession(sendDrafts); Visible = false; GetViewport().GuiReleaseFocus(); Closed?.Invoke(); }
    public void SelectApp(string id)
    {
        var app = _apps.Find(id, Device);
        if (app is null) { _receipt.Text = "Application unavailable on this device."; return; }
        _contentScroll.ScrollVertical = 0;
        _app = app.Id; _heading.Text = app.Label.ToUpperInvariant();
        foreach (var (key, button) in _navigation) button.SetPressedNoSignal(key == _app);
        _command.Text = ""; Refresh();
    }
    public void Send(PersonnelTask task, Hex? destination = null)
    {
        var actor = _sim.Actors.Where(a => a != _sim.Player).ElementAt(_recipient.Selected);
        var d = _sim.Operations.Definition!;
        var target = destination ?? (task == PersonnelTask.Transport ? d.Home : task == PersonnelTask.Post ? d.Posts[_recipient.Selected % d.Posts.Length] : _sim.Player.Cell);
        if (Device == OscpDevice.Phone && _sim.ManualPause)
        {
            if (!_sim.OscpAccess(Device).CanPlan) { _receipt.Text = "Cannot prepare this command with the current authority or connection."; return; }
            _phoneDrafts.RemoveAll(draft => draft.ActorId == actor.Id);
            _phoneDrafts.Add(new(actor.Id, task, target));
            _receipt.Text = $"Prepared {task} for {actor.Name}. Close phone to send ({_phoneDrafts.Count} draft(s)).";
            Refresh(); return;
        }
        _sim.IssueOscpOrder(Device, actor.Id, task, target); _receipt.Text = _sim.LastMessage;
    }
    public void Execute(string command)
    {
        _command.Text = "";
        var parts = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        var app = _apps.Find(command.Trim(), Device);
        if (app is not null) { SelectApp(app.Id); _receipt.Text = "Opened " + app.Label; return; }
        if (parts.Length == 1 && parts[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        { _receipt.Text = "Open: status, team, records, mail, map. Orders: order <staff id> <observe|follow|post|recover> [q r]."; return; }
        if (parts.Length is 3 or 5 && parts[0].Equals("order", StringComparison.OrdinalIgnoreCase))
        {
            var index = Array.FindIndex(_sim.Actors.Where(a => a != _sim.Player).ToArray(), a => a.Id.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
            var task = parts[2].ToLowerInvariant() switch { "observe" => PersonnelTask.Observe, "follow" => PersonnelTask.Follow, "post" => PersonnelTask.Post, "recover" => PersonnelTask.Transport, _ => (PersonnelTask)(-1) };
            if (index >= 0 && Enum.IsDefined(task) && (parts.Length == 3 || task == PersonnelTask.Post && int.TryParse(parts[3], out _) && int.TryParse(parts[4], out _)))
            {
                _recipient.Select(index); Send(task, parts.Length == 5 ? new(int.Parse(parts[3]), int.Parse(parts[4])) : null); return;
            }
        }
        _receipt.Text = "Unknown command or unavailable application. Type help.";
    }
    public void HandleInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.Escape || key.Keycode == (Device == OscpDevice.Phone ? Key.P : Key.T)) Close();
            else if (key.Keycode == Key.Space) { _ownsPhonePause = false; _sim.ManualPause = !_sim.ManualPause; }
        }
        GetViewport().SetInputAsHandled();
    }
    public void Refresh()
    {
        if (!Visible) return;
        var access = _sim.OscpAccess(Device);
        if (!access.Available) { CloseSession(false); return; }
        var phone = Device == OscpDevice.Phone;
        var viewport = GetViewportRect().Size;
        _frame.Size = phone ? new(Math.Min(490, viewport.X - 30), Math.Min(820, viewport.Y - 32))
            : new(Math.Min(1320, viewport.X - 28), Math.Min(860, viewport.Y - 28));
        _frame.Position = (viewport - _frame.Size) / 2;
        var area = _frame.ScreenArea; _screen.Position = area.Position; _screen.Size = area.Size;
        _frame.QueueRedraw();
        _nav.CustomMinimumSize = new(phone ? 95 : 164, 0);
        _body.AddThemeConstantOverride("separation", phone ? 10 : 20);
        _subtitle.Text = phone ? "" : "INFORMATION & OPERATIONS";
        _session.Text = $"{access.DeviceId} / {access.Identity} / {access.Role}";
        _link.Text = access.Status;
        var time = TimeSpan.FromHours(8) + TimeSpan.FromMilliseconds(_sim.WorldMs);
        _clock.Text = $"{time:hh\\:mm\\:ss}  {_sim.Speed}×  · " + (_sim.ManualPause ? _ownsPhonePause ? "PHONE PAUSE" : "MANUAL PAUSE" : _sim.LocalProgressHeld ? "LOCAL WAIT · calendar continues" : "REAL TIME · site continues");
        var view = _apps.Find(_app, Device)?.View;
        _toolbar.Visible = _actions.Visible = view == "personnel";
        foreach (var button in _actions.GetChildren().SelectMany(n => n.GetChildren()).OfType<Button>()) button.Disabled = !(Device == OscpDevice.Phone ? access.CanPlan : access.CanCommand);
        _clearDrafts.Visible = Device == OscpDevice.Phone && _phoneDrafts.Count > 0;
        _map.Visible = view == "map";
        _map.QueueRedraw();
        _scope.Text = view == "personnel" ? "Assigned site personnel · acknowledgement and travel remain separate" : view == "map" ? "Known plan · your position · recorded contacts; no live tracking" : access.Connected ? "Received information / current session" : "Offline / last acquired information";
        var reports = _sim.OscpRecords.Reverse().Select(r => $"L {r.LocalMs / 1000.0:0.0}s  /  {r.Author}  /  {r.Source.ToUpperInvariant()}\n{r.Text}");
        var own = _sim.Operations.OrderHistory.Where(o => o.Issuer == _sim.Player.Id && (o.IssuedMs > 0 || o.Channel is "phone" or "terminal")).TakeLast(6).Reverse()
            .Select(o => $"SUBMITTED #{o.Id}  {o.ActorId} / {o.Task} / {o.Channel}");
        var text = view switch
        {
            "operations" => "OBSERVATION SHIFT / INCIDENT 650\n\n" + (_sim.OscpRecords.LastOrDefault(r => r.Text.StartsWith("Containment assessment:") || r.Text.StartsWith("Receiving footprint empty"))?.Text ?? "Awaiting a received containment assessment.")
                + "\n\nCOMMAND REGISTER\n" + (own.Any() ? string.Join("\n", own) : "No new orders submitted.") + "\n\nOpen PERSONNEL to direct assigned staff. COMMUNICATIONS carries delivered acknowledgements and field reports.",
            "personnel" => string.Join("\n\n", _sim.Actors.Where(a => a != _sim.Player).Select(a => a.Name + " / " + a.Id + "\nAssigned to current site operation"))
                + "\n\n" + string.Join("\n", own) + DraftDescription() + "\n\nAn accepted command does not establish coverage. Wait for the employee's acknowledgement and subsequent report.",
            "records" => "SCP-650 / CONTAINMENT BRIEF\n\nMaintain a lit, secured receiving room and continuous human observation. Site procedure calls for three personnel. A single qualifying observer may prevent self-relocation while staffing remains inadequate.\n\nIf found outside containment, establish observation and coordinate recovery. The current procedure uses an open trolley with separate observers. The entity has no demonstrated attack routine.\n\nOld reports and unattended recordings do not establish current observation.\n\nPROCEDURE NOTE\nReport an empty receiving area as an absence. Do not infer the subject's new location.\n\nSource: SCP-650 by Dr Talson / SCP Wiki, CC BY-SA 3.0. This operational brief includes the game's transport adaptation.",
            "communications" => "RECEIVED RECORDS\n\n" + (reports.Any() ? string.Join("\n\n", reports) : "No acquired messages."),
            "map" => "Contacts mark recorded positions. They can become stale. Door markings identify planned access points, not current open/closed status.",
            "systems" => $"LOCAL WORKSTATION\n\nPower: {(_sim.Operations.TerminalPowered ? "available" : "unavailable")}\nSite connection: {(access.Connected ? "connected" : "offline")}\n\nSESSION\n{access.Identity}\n{access.Role}\n\nACCESS\nThis workstation's operations service validates authority and delivery for every command.\n\nDISPLAY\nUse the brightness control above to adjust the screen.\n\nOSCP interface inspiration: SCP-245 (Doctor Cimmerian) and SCP-5810 (Lt Flops), SCP Wiki / CC BY-SA 3.0. Hardware surround and applications are original game adaptations.",
            _ => "This application has no installed view handler."
        };
        if (_document.Text != text) _document.Text = text;
    }
}
