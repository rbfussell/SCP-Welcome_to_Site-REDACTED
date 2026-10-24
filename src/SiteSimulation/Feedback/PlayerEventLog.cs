namespace SiteSimulation;

public enum MessageKind { Observation, Orders, Combat, Equipment, Conversation, Status, Warning }
public sealed record PlayerMessage(long Id, long WorldMs, long LocalMs, MessageKind Kind, string Text);
public sealed record EventLogSnapshot(long NextId, PlayerMessage[] Entries, string[] Consumed, string[] Visible);

/// <summary>Acquired player-facing events only. Never populated by copying the authoritative debug trace.</summary>
public sealed class PlayerEventLog
{
    public const int Capacity = 100;
    private readonly List<PlayerMessage> _entries = [];
    private readonly HashSet<string> _consumed = [];
    private readonly Queue<string> _consumedOrder = [];
    internal HashSet<string> Visible { get; } = [];
    private long _nextId = 1;
    public IReadOnlyList<PlayerMessage> Entries => _entries.AsReadOnly();
    public long Revision => _nextId;
    public void Add(long worldMs, long localMs, MessageKind kind, string text)
    {
        if (!Enum.IsDefined(kind) || string.IsNullOrWhiteSpace(text) || text.Length > 2000 || worldMs < localMs || localMs < 0)
            throw new ArgumentException("Invalid player message.");
        _entries.Add(new(_nextId++, worldMs, localMs, kind, text));
        if (_entries.Count > Capacity) _entries.RemoveAt(0);
    }
    internal bool Once(string key)
    {
        if (!_consumed.Add(key)) return false;
        _consumedOrder.Enqueue(key);
        if (_consumedOrder.Count > 4096) _consumed.Remove(_consumedOrder.Dequeue());
        return true;
    }
    public PlayerMessage[] Window(long? firstId = null, int count = 5)
    {
        if (count is < 1 or > Capacity) throw new ArgumentOutOfRangeException(nameof(count));
        var start = firstId is { } id ? (_entries.FindIndex(e => e.Id >= id) is var found && found >= 0 ? found : Math.Max(0, _entries.Count - count)) : Math.Max(0, _entries.Count - count);
        return _entries.Skip(start).Take(count).ToArray();
    }
    internal EventLogSnapshot Capture() => new(_nextId, _entries.ToArray(), _consumedOrder.ToArray(), Visible.Order(StringComparer.Ordinal).ToArray());
    internal void Restore(EventLogSnapshot snapshot, long worldMs, long localMs, IReadOnlySet<string> subjects)
    {
        if (snapshot is null || snapshot.Entries is null || snapshot.Consumed is null || snapshot.Visible is null
            || snapshot.NextId < 1 || snapshot.Entries.Length > Capacity || snapshot.Consumed.Length > 4096
            || snapshot.Visible.Any(id => !subjects.Contains(id)) || snapshot.Visible.Distinct().Count() != snapshot.Visible.Length
            || snapshot.Consumed.Any(k => string.IsNullOrWhiteSpace(k) || k.Length > 180)
            || snapshot.Consumed.Distinct().Count() != snapshot.Consumed.Length
            || snapshot.Entries.Any(e => e is null || e.Id < 1 || e.Id >= snapshot.NextId || e.WorldMs < 0 || e.WorldMs > worldMs
                || e.LocalMs < 0 || e.LocalMs > localMs || e.LocalMs > e.WorldMs || !Enum.IsDefined(e.Kind)
                || string.IsNullOrWhiteSpace(e.Text) || e.Text.Length > 2000)
            || !snapshot.Entries.Select(e => e.Id).SequenceEqual(snapshot.Entries.Select(e => e.Id).Order().Distinct()))
            throw new ArgumentException("Invalid player event log.");
        _entries.Clear(); _entries.AddRange(snapshot.Entries); _nextId = snapshot.NextId;
        _consumed.Clear(); _consumedOrder.Clear(); foreach (var key in snapshot.Consumed) Once(key); Visible.Clear(); Visible.UnionWith(snapshot.Visible);
    }
}
