using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Local voice controls and a bounded OSCP command/document view over received information.</summary>
public partial class OperationsPanel : VBoxContainer
{
    private SpatialSimulation _sim = null!;
    private OptionButton _recipient = null!;
    private Label _summary = null!, _access = null!, _channel = null!, _directory = null!;
    private RichTextLabel _document = null!;
    private VBoxContainer _terminal = null!;
    public bool TerminalOpen { get; private set; }
    public bool ChoosingPost { get; private set; }
    public int SelectedRecipient { get => _recipient.Selected; set => _recipient.Select(value); }
    public void Initialize(SpatialSimulation sim)
    {
        _sim = sim;
        AddThemeConstantOverride("separation", 9);
        if (sim.Operations.Definition is null) { Visible = false; return; }
        AddLabel("SITE OPERATIONS", 17);
        _summary = AddLabel("", 12);
        _access = AddLabel("", 12);
        AddButton("Approach OSCP terminal", sim.ApproachTerminal);
        AddButton("Use / close OSCP     T", ToggleTerminal);
        _channel = AddLabel("", 12);
        _recipient = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.None };
        foreach (var actor in sim.Actors.Where(a => a != sim.Player)) _recipient.AddItem(actor.Name);
        AddChild(_recipient);
        AddButton("Maintain observation", () => Send(PersonnelTask.Observe));
        AddButton("Follow me", () => Send(PersonnelTask.Follow));
        AddButton("Take corridor post", () => Send(PersonnelTask.Post));
        AddButton("Choose observation post on map", () => ChoosingPost = true);
        AddButton("Recover SCP-650 using trolley", () => Send(PersonnelTask.Transport));
        _terminal = new VBoxContainer(); AddChild(_terminal);
        _directory = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; _terminal.AddChild(_directory);
        _document = new RichTextLabel { FitContent = true, ScrollActive = false, BbcodeEnabled = false, CustomMinimumSize = new(260, 80) };
        _document.AddThemeFontSizeOverride("normal_font_size", 12); _terminal.AddChild(_document);
        AddLabel("Trolley work needs another observer.\nReceiving: three humans, light, closed access, 3s stabilization.\n\nOrders wait for delivery. Clear access and restore coverage when staff report a blockage.", 12);
    }
    public void ToggleTerminal()
    {
        if (TerminalOpen) { TerminalOpen = false; return; }
        if (_sim.CanUseTerminal) TerminalOpen = true;
    }
    public void Send(PersonnelTask task)
    {
        var d = _sim.Operations.Definition!;
        var actor = _sim.Actors.Where(a => a != _sim.Player).ElementAt(_recipient.Selected);
        var destination = task == PersonnelTask.Transport ? d.Home : task == PersonnelTask.Post ? d.Posts[_recipient.Selected % d.Posts.Length] : actor.Cell;
        _sim.IssueOrder(actor.Id, task, destination, TerminalOpen);
    }
    public void PlacePost(Hex destination)
    {
        var actor = _sim.Actors.Where(a => a != _sim.Player).ElementAt(_recipient.Selected);
        if (_sim.IssueOrder(actor.Id, PersonnelTask.Post, destination, TerminalOpen)) ChoosingPost = false;
    }
    public void CancelPost() => ChoosingPost = false;
    public void Refresh()
    {
        if (_sim.Operations.Definition is not { } d) return;
        if (TerminalOpen && !_sim.CanUseTerminal) TerminalOpen = false;
        var assessment = _sim.Operations.ReceivedReports.LastOrDefault(r => (TerminalOpen || r.Source == "voice")
            && (r.Text.StartsWith("Containment assessment:") || r.Text.StartsWith("Receiving footprint empty")));
        _summary.Text = assessment is null ? "Awaiting a delivered containment assessment" : $"LAST REPORT / L {assessment.LocalMs / 1000.0:0.0}s\n{assessment.Text}";
        _access.Text = _sim.CanUseTerminal ? "OSCP terminal within reach · service online" : "OSCP: approach the terminal; power/network required";
        _channel.Text = TerminalOpen ? "OSCP / COMMAND · site radio delivery" : "LOCAL VOICE · requires proximity and clear path";
        if (ChoosingPost) _channel.Text += "\nClick a post on the map · RMB cancels";
        _terminal.Visible = TerminalOpen;
        if (!TerminalOpen) return;
        _directory.Text = "OSCP / INCIDENT 650\nAssigned staff: " + string.Join(", ", _sim.Actors.Where(a => a != _sim.Player).Select(a => a.Name));
        // Submission intent is known to the issuer. Execution status is shown only through delivered reports.
        var submissions = _sim.Operations.OrderHistory.Where(o => o.IssuedMs > 0).TakeLast(4)
            .Select(o => $"SUBMITTED #{o.Id} · {o.ActorId} · {o.Task}");
        var reports = _sim.Operations.ReceivedReports.TakeLast(12).Reverse()
            .Select(r => $"L {r.LocalMs / 1000.0:0.0}s / {r.Author}\n{r.Text}");
        _document.Text = string.Join("\n", submissions) + "\n\nRECEIVED RECORDS\n\n" + string.Join("\n\n", reports);
    }
    private Label AddLabel(string text, int size)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size); AddChild(label); return label;
    }
    private void AddButton(string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.AddThemeFontSizeOverride("font_size", 12); button.Pressed += action; AddChild(button);
    }
}
