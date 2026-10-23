using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Debug projections and controls. No perception rules, random draws or world mutations in Refresh.</summary>
public partial class SpatialHud : Control
{
    public Control Field { get; private set; } = null!;
    public Action SaveRequested { get; set; } = () => { };
    public Action RestoreRequested { get; set; } = () => { };
    public Action ResetCameraRequested { get; set; } = () => { };
    public Action ExportTraceRequested { get; set; } = () => { };
    private TabContainer _tabs = null!;
    private VBoxContainer _fieldPanel = null!;
    private ScrollContainer _fieldScroll = null!;
    private CheckButton _stopDirect = null!, _stopReports = null!;
    private Label _viewHint = null!, _fieldMode = null!;
    private Control _footer = null!;
    private Label _clock = null!, _mode = null!, _status = null!, _coordinates = null!, _timers = null!;
    private Label _anomaly = null!, _observers = null!, _rule = null!, _witnesses = null!;
    private RichTextLabel _trace = null!;
    private readonly Dictionary<Key, CheckButton> _toggles = [];
    private static readonly Color Text = new("e4e3d2"), Muted = new("97a7a0"), Green = new("a0d1af");

    public void Initialize(SpatialSimulation sim, WorldView world)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Theme = new Theme { DefaultFontSize = 13 };
        Theme.SetColor("font_color", "Label", Text);
        Theme.SetColor("default_color", "RichTextLabel", Text);
        Theme.SetColor("font_color", "Button", Text);
        Theme.SetStylebox("normal", "Button", Box("1c2927", "3a4d43"));
        Theme.SetStylebox("hover", "Button", Box("2c4035", "9ac5a4"));
        Theme.SetStylebox("pressed", "Button", Box("374e3d", "9ac5a4"));
        Theme.SetStylebox("panel", "TabContainer", Box("111c1b", "304039"));
        At(this, "FOUNDATION  /  SITE [REDACTED]", new(28, 21), 25, Text);
        At(this, "OBSERVATION LAB     /     SCP-650 NATIVE MODULE     /     M0 PROTOTYPE", new(30, 61), 12, Green);
        _clock = At(this, "", new(930, 27), 17, Text);
        var viewSwitch = new Button { Text = "FIELD / DIAGNOSTICS  F2", Position = new(30, 94), FocusMode = FocusModeEnum.None };
        viewSwitch.Pressed += () => { world.FieldView = !world.FieldView; ResetCameraRequested(); }; AddChild(viewSwitch);
        _viewHint = At(this, "", new(260, 101), 11, Muted);
        Field = new Control { Position = new(20, 124), ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(Field); Field.AddChild(world);
        _tabs = new TabContainer(); AddChild(_tabs);
        var view = Tab("View");
        Line(view, "SPATIAL CONTROLS", 12, Green);
        _mode = Line(view, "", 12, Green);
        _toggles[Key.C] = Toggle(view, "White collision boundaries  [C]", true, value => world.CollisionOverlay = value);
        _toggles[Key.G] = Toggle(view, "Actor hex grid  [G]", false, value => world.HexGrid = value);
        _toggles[Key.B] = Toggle(view, "Square floor grid  [B]", false, value => world.FloorGrid = value);
        _toggles[Key.L] = Toggle(view, "Directional sight  [L]", true, value => world.SightOverlay = value);
        _toggles[Key.H] = Toggle(view, "Wall cutaway  [H]", true, value => world.Cutaway = value);
        Button(view, "Use nearest door     E", () => sim.ToggleNearestDoor());
        Button(view, "Local hold / release     F6", () => sim.LocalHold = !sim.LocalHold);
        Button(view, "Global manual pause     Space", () => sim.ManualPause = !sim.ManualPause);
        var speeds = new HBoxContainer(); view.AddChild(speeds);
        foreach (var speed in new[] { 1, 2, 4 }) Button(speeds, $"{speed}×", () => sim.Speed = speed);
        _timers = Line(view, "", 12, Muted);
        Button(view, "Save     F5", () => SaveRequested());
        Button(view, "Restore     F9", () => RestoreRequested());
        Button(view, "Reset camera     Home", () => ResetCameraRequested());
        Line(view, "White: blocked geometry\nMint: sight / route / eligible observer\nAll local timers wait during F6 hold.", 11, Muted);
        var anomaly = Tab("SCP-650");
        Line(anomaly, "ANOMALY INSPECTOR", 12, Green);
        _anomaly = Line(anomaly, "", 16, Text);
        _rule = Line(anomaly, "", 12, Green);
        _observers = Line(anomaly, "", 12, Text);
        _witnesses = Line(anomaly, "", 12, Muted);
        anomaly.AddChild(new HSeparator());
        Button(anomaly, "Chen attention on / off     O", sim.ToggleObserverAttention);
        Button(anomaly, "Blink avatar     K", sim.BlinkPlayer);
        Button(anomaly, "Lighting on / off     F8", sim.ToggleLighting);
        Button(anomaly, "Avatar complacency on / off     J", () => ToggleComplacency(sim));
        Button(anomaly, "Local hold / release     F6", () => sim.LocalHold = !sim.LocalHold);
        Line(anomaly, "These are debug fixture controls.\n\nOne eligible human freezes self-relocation. Three-person staffing is a separate requirement.\n\nTurn toward a newly found statue to hold it. It has no attack routine.", 11, Muted);
        var trace = Tab("Trace");
        Line(trace, "CAUSAL EVENT TRACE", 12, Green);
        Button(trace, "Export trace JSON", () => ExportTraceRequested());
        _trace = new RichTextLabel { FitContent = true, ScrollActive = false, CustomMinimumSize = new(265, 100), BbcodeEnabled = false };
        _trace.AddThemeFontSizeOverride("normal_font_size", 11); trace.AddChild(_trace);
        _tabs.CurrentTab = 1;
        _fieldScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; AddChild(_fieldScroll);
        _fieldPanel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new(280, 0) };
        _fieldScroll.AddChild(_fieldPanel);
        _fieldPanel.AddThemeConstantOverride("separation", 16);
        Line(_fieldPanel, "OBSERVATION SHIFT", 18, Text);
        _fieldMode = Line(_fieldPanel, "", 12, Green);
        _stopDirect = Toggle(_fieldPanel, "Stop on my anomaly sightings", sim.Awareness.StopOnDirectSight, value => sim.Awareness.StopOnDirectSight = value);
        _stopReports = Toggle(_fieldPanel, "Stop on communicated sightings", sim.Awareness.StopOnReports, value => sim.Awareness.StopOnReports = value);
        Line(_fieldPanel, "Keep it in sight.\n\nThe facility plan is known. People and SCP-650 are shown only when your character can see them. Looking away matters.", 14, Muted);
        Line(_fieldPanel, "PROTOTYPE ENCOUNTER\n\nO  — interrupt Chen's watch\nRMB / Ctrl-click  — turn\nK  — blink\nE  — use nearest door\nShift-click  — move, keep facing", 13, Text);
        Button(_fieldPanel, "Interrupt / restore Chen's watch   O", sim.ToggleObserverAttention);
        Button(_fieldPanel, "Global pause   Space", () => sim.ManualPause = !sim.ManualPause);
        Line(_fieldPanel, "F2   Diagnostics and module state\nF4   Art / diagnostic statue\nM    Mute audio\nR     Reduce effects\nF5 / F9   Save / restore\nHome   Reset camera", 12, Muted);
        Line(_fieldPanel, "Encounter audio is presentation only.\nNo flashing or camera shake.\n\nComplacency is a per-person module flag, switched with J for testing. Its future metric is undecided.", 11, Muted);
        _footer = new Control { MouseFilter = MouseFilterEnum.Ignore }; AddChild(_footer);
        _status = At(_footer, "", new(28, 0), 13, Text);
        _coordinates = At(_footer, "", new(28, 25), 11, Green);
        At(_footer, "Click move · RMB face · Shift-click keep facing · O observer · K blink · F2 diagnostics · M mute · R reduced effects", new(28, 47), 12, Muted);
        Layout();
    }
    public static void ToggleComplacency(SpatialSimulation sim)
    {
        var status = sim.InspectAnomalies().First();
        sim.SetModuleFlag(status.Id, "complacency", sim.Player.Id, !status.FlaggedActors("complacency").Contains(sim.Player.Id));
    }
    private VBoxContainer Tab(string title)
    {
        var scroll = new ScrollContainer { Name = title, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _tabs.AddChild(scroll);
        var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(margin);
        foreach (var side in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 12);
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; column.AddThemeConstantOverride("separation", 9);
        margin.AddChild(column); return column;
    }
    private static StyleBoxFlat Box(string background, string border) => new()
    {
        BgColor = new(background), BorderColor = new(border), BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
        ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6
    };
    private static Label Line(Node parent, string text, int size, Color color)
    {
        var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(260, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color); parent.AddChild(label); return label;
    }
    private static Label At(Node parent, string text, Vector2 position, int size, Color color)
    { var label = Line(parent, text, size, color); label.AutowrapMode = TextServer.AutowrapMode.Off; label.CustomMinimumSize = Vector2.Zero; label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; label.Position = position; return label; }
    private static void Button(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.AddThemeFontSizeOverride("font_size", 12); button.Pressed += action; parent.AddChild(button);
    }
    private static CheckButton Toggle(Node parent, string text, bool initial, Action<bool> action)
    {
        var button = new CheckButton { Text = text, ButtonPressed = initial, FocusMode = FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 12); button.Toggled += value => action(value); parent.AddChild(button); return button;
    }
    public void Layout()
    {
        var size = GetViewportRect().Size;
        Field.Size = new(Math.Max(500, size.X - 372), Math.Max(350, size.Y - 235));
        _tabs.Position = new(size.X - 338, 103); _tabs.Size = new(318, size.Y - 126);
        _fieldScroll.Position = new(size.X - 324, 137); _fieldScroll.Size = new(304, size.Y - 160);
        _footer.Position = new(0, size.Y - 88); _clock.Position = new(size.X - 440, 27);
    }
    public void Refresh(SpatialSimulation sim, WorldView world, Hex? hover, Point2 ground, float zoom, string? notice)
    {
        var time = TimeSpan.FromHours(8) + TimeSpan.FromMilliseconds(sim.WorldMs);
        _clock.Text = $"WORLD  {time:hh\\:mm\\:ss}     {sim.Speed}×";
        _mode.Text = sim.ManualPause ? "MANUAL PAUSE · all areas" : sim.LocalHold ? "LOCAL HELD · world continues" : "REAL TIME · local active";
        _fieldMode.Text = _mode.Text;
        _tabs.Visible = !world.FieldView; _fieldScroll.Visible = world.FieldView;
        _stopDirect.SetPressedNoSignal(sim.Awareness.StopOnDirectSight); _stopReports.SetPressedNoSignal(sim.Awareness.StopOnReports);
        _viewHint.Text = world.FieldView ? "FIELD VIEW · direct character sight · F2 exposes diagnostics" : "DEBUG VIEW · full visibility · inspector is not an observer";
        _status.Text = notice ?? sim.LastMessage;
        _toggles[Key.C].SetPressedNoSignal(world.CollisionOverlay); _toggles[Key.G].SetPressedNoSignal(world.HexGrid);
        _toggles[Key.B].SetPressedNoSignal(world.FloorGrid); _toggles[Key.L].SetPressedNoSignal(world.SightOverlay); _toggles[Key.H].SetPressedNoSignal(world.Cutaway);
        _coordinates.Text = hover is { } cell ? $"HEX {cell.Q},{cell.R}   FLOOR {Math.Floor(ground.X)},{Math.Floor(ground.Y)}   {(sim.Map.Walkable(cell) ? "FREE" : "BLOCKED")}   ZOOM {zoom:0.00}×" : "FLOOR / HEX / SPRITE anchors remain separate";
        _timers.Text = $"Test timer: {sim.LocalCountdownMs / 1000.0:0.00}s\nWorld: {sim.WorldMs / 1000.0:0.0}s  Local: {sim.LocalMs / 1000.0:0.0}s\nOff-site updates: {sim.OffsiteUpdates}";
        var status = sim.InspectAnomalies().First();
        var body = sim.Anomalies.Bodies.First();
        _anomaly.Text = $"{status.Id} · pose {status.Pose + 1}/{body.PoseCount}\nHEX {body.Cell.Q},{body.Cell.R}";
        _rule.Text = $"{(sim.LocalHold || sim.ManualPause ? "LOCAL PROGRESS HELD\n" : "")}{status.Rule}\nEligible: {status.Observation.EligibleIds.Length} ({status.Observation.Profile})\nStaffing count: {status.Observation.EligibleIds.Length} / 3\nRelocations: {status.Relocations}\nLast candidate count: {status.Candidates}\nNext evaluation in {Math.Max(0, status.NextEvaluationMs - sim.LocalMs) / 1000.0:0.00}s local\nComplacency: {(status.FlaggedActors("complacency").Length == 0 ? "none" : string.Join(", ", status.FlaggedActors("complacency")))}\nMetric: manual authoring flag only";
        _observers.Text = string.Join("\n\n", status.Observation.Observers.Select(o => $"{o.ActorId} / {o.Kind}\n{o.Reason}{(o.Detected && o.Eligible ? " · HOLDS" : "")}"));
        _witnesses.Text = $"Lighting: {(sim.Map.LightsOn ? "on" : "off")}\nDiscovered this encounter:\n{(status.RecentWitnesses.Length == 0 ? "none" : string.Join(", ", status.RecentWitnesses))}";
        _trace.Text = string.Join("\n\n", sim.Trace.Entries.Reverse().Select(e => $"#{e.Sequence}  L{e.LocalMs / 1000.0:0.00}s W{e.WorldMs / 1000.0:0.00}s\n{e.System} / {e.Subject}\n{e.Event}: {e.Detail}"));
    }
}
