using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Optional sprite qualification view. It reads the real outfit; normal paid closure/context actions remain authoritative.</summary>
public partial class InventoryPaperdollProof : VBoxContainer
{
    public InventoryPanel Panel { get; set; } = null!;
    internal InventoryPaperdollArt Art { get; private set; } = null!;
    internal InventoryPaperdollSurface Surface { get; private set; } = null!;
    internal string Side { get; private set; } = "front";
    internal Button Flip { get; private set; } = null!;
    internal Button Jacket { get; private set; } = null!;
    private Label _limits = null!;
    private string? _jacket;
    public override void _Ready()
    {
        Art = new InventoryPaperdollArt();
        InventoryPanel.Label(this, "FULL-BODY SPRITE PROOF", 13);
        Surface = new InventoryPaperdollSurface { Proof = this, CustomMinimumSize = new(234, 350), TextureFilter = TextureFilterEnum.Nearest }; AddChild(Surface);
        Flip = InventoryPanel.Button(this, "Front · flip", () => { Side = Side == "front" ? "back" : "front"; Surface.ClearHover(); Refresh(); });
        Jacket = InventoryPanel.Button(this, "Jacket", () => { if (_jacket is not null) Panel.ToggleClosure(_jacket); });
        InventoryPanel.Label(this, "Click selects · double-click inspects\nRight-click actions · fit/move in hex view", 11);
        _limits = InventoryPanel.Label(this, "", 11); _limits.AutowrapMode = TextServer.AutowrapMode.WordSmart; _limits.CustomMinimumSize = new(234, 95);
        Refresh();
    }
    internal void ResetView() { Side = "front"; Surface.ClearHover(); }
    internal void Refresh()
    {
        if (!IsNodeReady() || !Visible) return;
        Flip.Text = Side == "front" ? "Front · flip" : "Back · flip";
        _jacket = Art.Layers(Panel.Simulation).FirstOrDefault(l => l.Column is 2 or 3)?.ItemId;
        Jacket.Disabled = _jacket is null; Jacket.Text = "No bound jacket worn";
        if (_jacket is { } id)
        {
            var closure = InventoryClosures.Toggle(Panel.Simulation.Inventory, id);
            var quote = Panel.Simulation.QuoteInventory(closure.Request);
            Jacket.Text = closure.Verb + " jacket"; Jacket.Disabled = !quote.Allowed || Panel.Simulation.Inventory.Pending is not null;
            Jacket.TooltipText = Panel.Simulation.InventoryCost(closure.Request);
        }
        var omitted = Art.Omitted(Panel.Simulation);
        _limits.Text = "One reference body; other gear omitted:\n" + (omitted.Length == 0 ? "none" : string.Join(", ", omitted))
            + "\nModesty base is visual only.\nWorld outfit remains baked artwork.";
        Surface.QueueRedraw();
    }
    public override void _ExitTree() => Art?.Dispose();
}

/// <summary>Pixel-alpha picking and tint share the atlas's exact letterbox transform, including front/back artwork.</summary>
public partial class InventoryPaperdollSurface : Control
{
    public InventoryPaperdollProof Proof { get; set; } = null!;
    internal string? Hovered { get; private set; }
    private Shader? _cutoutShader;
    private ShaderMaterial? _cutoutMaterial;
    internal string? Pick(Vector2 at) => Proof.Art.Pick(Proof.Panel.Simulation, Proof.Side, Size, at);
    internal void ClearHover() { Hovered = null; TooltipText = ""; MouseDefaultCursorShape = CursorShape.Arrow; QueueRedraw(); }
    public override void _Ready() { MouseFilter = MouseFilterEnum.Stop;
        _cutoutShader = new Shader { Code = "shader_type canvas_item; void fragment() { if (COLOR.a <= 0.1) { discard; } }" };
        _cutoutMaterial = new ShaderMaterial { Shader = _cutoutShader }; Material = _cutoutMaterial;
        MouseExited += ClearHover; }
    public override void _ExitTree() { Material = null; _cutoutMaterial?.Dispose(); _cutoutShader?.Dispose(); }
    public override void _Process(double delta)
    {
        if (!IsVisibleInTree() || GetViewport().GuiGetHoveredControl() != this) return;
        Hovered = Pick(GetLocalMousePosition());
        TooltipText = Hovered is { } id ? Proof.Panel.Describe(id) + "\n" + Proof.Panel.Simulation.InventoryMoveAccess(id).Reason : "Reference body / visual modesty base or transparent space.";
        MouseDefaultCursorShape = Hovered is null ? CursorShape.Arrow : CursorShape.PointingHand;
    }
    public override void _Draw() => Proof.Art.Draw(this, Proof.Panel.Simulation, Proof.Side, Hovered, Proof.Panel.SelectedItem);
    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true } mouse || Pick(mouse.Position) is not { } id) return;
        if (mouse.ButtonIndex == MouseButton.Left) { Proof.Panel.Select(id); if (mouse.DoubleClick) Proof.Panel.Inspect(id); }
        else if (mouse.ButtonIndex == MouseButton.Right) Proof.Panel.Context(id, null, GetGlobalMousePosition());
        AcceptEvent();
    }
}
