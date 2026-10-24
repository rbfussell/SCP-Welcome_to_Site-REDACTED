using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Five readable lines over a bounded journal. Scrolling anchors history while new messages arrive.</summary>
public partial class PlayerLogPanel : PanelContainer
{
    private readonly Label[] _lines = new Label[5];
    private Label _heading = null!;
    private VScrollBar _scroll = null!;
    private PlayerEventLog? _source;
    private long? _anchor;
    private long _revision = -1;
    private bool _updating;
    public void Initialize()
    {
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new("101a19"), BorderColor = new("415748"),
            BorderWidthTop = 1, ContentMarginLeft = 10, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6 });
        var row = new HBoxContainer(); AddChild(row);
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(column);
        column.AddThemeConstantOverride("separation", 1);
        _heading = new Label { Text = "EVENT LOG  /  100 entries", MouseFilter = MouseFilterEnum.Ignore };
        _heading.AddThemeFontSizeOverride("font_size", 11); column.AddChild(_heading);
        for (var i = 0; i < _lines.Length; i++)
        {
            _lines[i] = new Label { Text = " ", TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                CustomMinimumSize = new(0, 18), MouseFilter = MouseFilterEnum.Stop };
            _lines[i].AddThemeFontSizeOverride("font_size", 12); column.AddChild(_lines[i]);
        }
        _scroll = new VScrollBar { CustomMinimumSize = new(16, 0), Step = 1, Page = 5 }; row.AddChild(_scroll);
        _scroll.ValueChanged += value =>
        {
            if (_updating || _source is null) return;
            _anchor = value >= Math.Max(0, _source.Entries.Count - 5) ? null : _source.Entries[(int)value].Id;
            DrawEntries();
        };
        GuiInput += input =>
        {
            if (input is InputEventMouseButton { Pressed: true } mouse && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            { _scroll.Value += mouse.ButtonIndex == MouseButton.WheelUp ? -3 : 3; AcceptEvent(); }
        };
    }
    public void Refresh(PlayerEventLog source)
    {
        if (_source == source && _revision == source.Revision) return;
        if (_source != source) _anchor = null;
        _source = source; _revision = source.Revision; DrawEntries();
    }
    private void DrawEntries()
    {
        if (_source is null) return;
        var entries = _source.Window(_anchor);
        var start = entries.Length == 0 ? 0 : _source.Entries.ToList().FindIndex(e => e.Id == entries[0].Id);
        _updating = true; _scroll.MaxValue = Math.Max(5, _source.Entries.Count); _scroll.Value = Math.Max(0, start); _updating = false;
        _heading.Text = $"EVENT LOG  /  {_source.Entries.Count}/100  ·  {(_anchor is null ? "LIVE" : "HISTORY — scroll down for live")}";
        for (var i = 0; i < 5; i++)
        {
            var message = i < entries.Length ? entries[i] : null;
            _lines[i].Text = message is null ? " " : $"{TimeSpan.FromHours(8) + TimeSpan.FromMilliseconds(message.WorldMs):hh\\:mm\\:ss}  [{message.Kind.ToString().ToUpperInvariant()}]  {message.Text}";
            _lines[i].TooltipText = message?.Text ?? "";
            _lines[i].AddThemeColorOverride("font_color", message?.Kind switch
            {
                MessageKind.Combat => new("efad8e"), MessageKind.Observation => new("e4d393"),
                MessageKind.Orders => new("a7c9ed"), MessageKind.Equipment => new("aacfb4"),
                MessageKind.Conversation => new("d5b7e4"), MessageKind.Warning => new("f3bf6d"), _ => new Color("c3ceca")
            });
        }
    }
}
