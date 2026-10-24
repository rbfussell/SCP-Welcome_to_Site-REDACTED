namespace SiteSimulation;

public enum CombatActionKind { Move, Face, ReadyWeapon, Fire, Reload, Door, Wait }
public sealed record Combatant(string ActorId, string Side, int Initiative, int Ap = 0, int Points = 6,
    bool WeaponReady = false, int Ammo = 6, int ReserveAmmo = 12);
public sealed record CombatContact(string ObserverId, string TargetId, Point2 Position, long LocalMs);
public sealed record TacticalAction(long Id, string ActorId, CombatActionKind Kind, int Cost, int TotalMs, int RemainingMs,
    string? TargetId = null, Point2? TargetPoint = null, int CommittedSteps = 0, int CompletedSteps = 0);
public sealed record CombatSnapshot(bool Armed, bool Active, Combatant[] Participants, int Cursor, int Round,
    TacticalAction? Pending, long NextActionId, uint RandomState, CombatContact[] Contacts);

/// <summary>One local training encounter. Tuning and equipment are explicit fixtures, not full injury/inventory models.</summary>
public sealed class CombatState
{
    public const int TurnAp = 10, AttackCost = 4, ReadyCost = 2, ReloadCost = 3, FaceCost = 1;
    public const int EngagementHexes = 8, WeaponHexes = 6;
    public bool Armed { get; internal set; }
    public bool Active { get; internal set; }
    internal List<Combatant> Roster { get; } = [];
    internal List<CombatContact> Contacts { get; } = [];
    public IReadOnlyList<CombatContact> AcquiredContacts => Contacts.AsReadOnly();
    public IReadOnlyList<Combatant> Participants => Roster.AsReadOnly();
    public int Cursor { get; internal set; }
    public int Round { get; internal set; }
    public TacticalAction? Pending { get; internal set; }
    internal long NextActionId { get; set; } = 1;
    internal uint RandomState { get; set; } = 650173;
    public string? CurrentActorId => Active && Roster.Count > 0 ? Roster[Cursor].ActorId : null;
    public bool Waiting => Active && Pending is null;
    public Combatant? For(string actorId) => Roster.SingleOrDefault(c => c.ActorId == actorId);
    internal uint Roll()
    {
        var x = RandomState; x ^= x << 13; x ^= x >> 17; x ^= x << 5;
        return RandomState = x;
    }
    internal CombatSnapshot Capture() => new(Armed, Active, Roster.ToArray(), Cursor, Round, Pending, NextActionId, RandomState, Contacts.ToArray());
}
