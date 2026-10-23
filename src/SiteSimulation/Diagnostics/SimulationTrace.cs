namespace SiteSimulation;

public sealed record TraceEntry(long Sequence, long WorldMs, long LocalMs, string System, string Subject, string Event, string Detail);
public sealed record TraceSnapshot(long NextSequence, TraceEntry[] Entries);

/// <summary>Bounded causal diagnostics. Rendering/inspection never adds entries.</summary>
public sealed class SimulationTrace
{
    private readonly Queue<TraceEntry> _entries = new();
    private long _nextSequence = 1;
    public IReadOnlyList<TraceEntry> Entries => _entries.ToArray();
    public void Add(long worldMs, long localMs, string system, string subject, string eventName, string detail)
    {
        _entries.Enqueue(new(_nextSequence++, worldMs, localMs, system, subject, eventName, detail));
        while (_entries.Count > 96) _entries.Dequeue();
    }
    public TraceSnapshot Capture() => new(_nextSequence, _entries.ToArray());
    public void Restore(TraceSnapshot snapshot, long worldMs, long localMs)
    {
        if (snapshot is null || snapshot.Entries is null || snapshot.Entries.Length > 96 || snapshot.NextSequence < 1
            || snapshot.Entries.Any(e => e is null || e.Sequence < 1 || e.Sequence >= snapshot.NextSequence
                || e.WorldMs < 0 || e.WorldMs > worldMs || e.LocalMs < 0 || e.LocalMs > localMs)
            || !snapshot.Entries.Select(e => e.Sequence).SequenceEqual(snapshot.Entries.Select(e => e.Sequence).Order().Distinct()))
            throw new ArgumentException("Invalid diagnostic trace.");
        _entries.Clear();
        foreach (var entry in snapshot.Entries) _entries.Enqueue(entry);
        _nextSequence = snapshot.NextSequence;
    }
}
