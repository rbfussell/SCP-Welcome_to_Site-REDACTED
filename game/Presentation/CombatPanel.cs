using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>Explicit training fixture. The simulation alone grants turns and accepts AP transactions.</summary>
public partial class CombatPanel : VBoxContainer
{
    private SpatialSimulation _sim = null!;
    private Label _status = null!;
    private VBoxContainer _actions = null!;
    private Button _arm = null!;
    private OptionButton _opponent = null!;
    private OptionButton _target = null!;
    private string[] _opponents = [], _targets = [];
    public void Initialize(SpatialSimulation sim)
    {
        _sim = sim;
        AddThemeConstantOverride("separation", 5);
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; AddChild(_status);
        _opponent = new OptionButton { FocusMode = FocusModeEnum.None }; AddChild(_opponent);
        _opponents = sim.Actors.Where(a => a != sim.Player).Select(a => a.Id).ToArray();
        foreach (var id in _opponents) _opponent.AddItem(sim.Actors.Single(a => a.Id == id).Name + " · training opponent");
        _opponent.Select(Array.IndexOf(_opponents, "tech"));
        _arm = Button(this, "Arm nonlethal training drill", () => sim.ArmTraining(_opponents[_opponent.Selected]));
        _actions = new VBoxContainer(); AddChild(_actions);
        _target = new OptionButton { FocusMode = FocusModeEnum.None }; _actions.AddChild(_target);
        Button(_actions, "Ready sidearm  ·  2 AP  [V]", () => sim.UseTrainingEquipment(CombatActionKind.ReadyWeapon));
        Button(_actions, "Fire  ·  4 AP", () => { if (_target.Selected >= 0 && _target.Selected < _targets.Length) sim.FireTraining(_targets[_target.Selected]); });
        Button(_actions, "Reload  ·  3 AP  [Y]", () => sim.UseTrainingEquipment(CombatActionKind.Reload));
        Button(_actions, "End turn  [Enter]", () => sim.EndTurn());
        Button(_actions, "End training drill", () => sim.EndTraining());
        Refresh();
    }
    private static Button Button(Node parent, string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 12); button.Pressed += action; parent.AddChild(button); return button;
    }
    public void Refresh()
    {
        var combat = _sim.Combat; var player = combat.For(_sim.Player.Id);
        _arm.Visible = _opponent.Visible = !combat.Armed;
        _actions.Visible = combat.Armed;
        _status.Text = !combat.Armed ? "TACTICAL TRAINING  /  provisional equipment"
            : !combat.Active ? "DRILL ARMED · find the opponent by sight."
            : $"TRAINING · ROUND {combat.Round}\n{(combat.CurrentActorId == _sim.Player.Id ? "YOUR TURN" : "OTHER TURN")} · {(combat.Pending is { } action ? action.Kind.ToString().ToUpperInvariant() : "CHOOSING")}\nAP {player!.Ap}/10 · points {player.Points}/6\nSidearm: {(player.WeaponReady ? "ready" : "stowed")} · {player.Ammo}/6 + {player.ReserveAmmo} rounds\nMove 1/hex · turn 1 · door 2 AP";
        var targets = combat.Participants.Where(c => c.Side == "opposition" && c.Points > 0)
            .Select(c => _sim.Actors.Single(a => a.Id == c.ActorId)).Where(a => _sim.VisibleToPlayer(a.Position)).Select(a => a.Id).ToArray();
        if (!_targets.SequenceEqual(targets))
        {
            _targets = targets; _target.Clear();
            foreach (var id in targets) _target.AddItem(_sim.Actors.Single(a => a.Id == id).Name);
        }
        _target.Disabled = targets.Length == 0;
        _target.TooltipText = "Only directly visible training opponents appear here.";
    }
}
