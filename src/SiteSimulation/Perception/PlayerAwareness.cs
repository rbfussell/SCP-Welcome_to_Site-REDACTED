namespace SiteSimulation;

/// <summary>Position is acquired evidence or an estimate, never an implicit live entity lookup.</summary>
public sealed record PerceivedContact(string SubjectId, long Encounter, Point2 Position, long LocalMs, string Source, string ObserverId,
    string Classification = "anomaly", double UncertaintyRadius = 0);
public sealed record NoticedAnomaly(string SubjectId, long Encounter);
public sealed record AwarenessSnapshot(bool StopOnDirectSight, bool StopOnReports, PerceivedContact[] Evidence, NoticedAnomaly[] Noticed);

/// <summary>Player knowledge and configurable movement interruption. Independent of SCP modules and fear.</summary>
public sealed class PlayerAwareness
{
    private readonly Dictionary<string, PerceivedContact> _evidence = [];
    private readonly Dictionary<string, long> _noticed = [];
    public bool StopOnDirectSight { get; set; } = true;
    public bool StopOnReports { get; set; } = true;
    public IReadOnlyList<PerceivedContact> Evidence => _evidence.Values.OrderBy(e => e.SubjectId, StringComparer.Ordinal).ToArray();

    public bool Receive(PerceivedContact evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence.SubjectId) || evidence.Encounter < 0 || evidence.LocalMs < 0
            || string.IsNullOrWhiteSpace(evidence.ObserverId) || !double.IsFinite(evidence.Position.X) || !double.IsFinite(evidence.Position.Y)
            || evidence.Source is not ("direct" or "voice" or "radio" or "sound")
            || evidence.Classification is not ("anomaly" or "hostile" or "unknown")
            || !double.IsFinite(evidence.UncertaintyRadius) || evidence.UncertaintyRadius < 0)
            throw new ArgumentException("Invalid perceived contact evidence.");
        if (_evidence.TryGetValue(evidence.SubjectId, out var previous)
            && (evidence.LocalMs < previous.LocalMs || evidence.Encounter < previous.Encounter))
            return false;
        if (previous is null || evidence.LocalMs != previous.LocalMs || previous.Source != "direct" || evidence.Source == "direct")
            _evidence[evidence.SubjectId] = evidence;
        var enabled = evidence.Classification == "anomaly" && (evidence.Source == "direct" ? StopOnDirectSight : evidence.Source is "voice" or "radio" && StopOnReports);
        var fresh = !_noticed.TryGetValue(evidence.SubjectId, out var encounter) || evidence.Encounter != encounter;
        // A fresh known encounter is acknowledged even while stationary. Continuing sight is not another interruption.
        if (enabled) _noticed[evidence.SubjectId] = evidence.Encounter;
        return enabled && fresh;
    }

    public AwarenessSnapshot Capture() => new(StopOnDirectSight, StopOnReports, Evidence.ToArray(),
        _noticed.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new NoticedAnomaly(p.Key, p.Value)).ToArray());

    public void Restore(AwarenessSnapshot snapshot, IReadOnlyDictionary<string, long> subjects, IReadOnlySet<string> actors, long localMs)
    {
        bool ValidSubject(PerceivedContact evidence) => subjects.TryGetValue(evidence.SubjectId, out var current)
            ? evidence.Encounter >= 0 && evidence.Encounter <= current
            : !string.IsNullOrWhiteSpace(evidence.SubjectId) && evidence.Source == "sound" && evidence.Classification == "unknown" && evidence.Encounter == 0;
        if (snapshot.Evidence is null || snapshot.Noticed is null
            || snapshot.Evidence.Any(e => e is null || !ValidSubject(e) || e.LocalMs < 0 || e.LocalMs > localMs
                || !double.IsFinite(e.Position.X) || !double.IsFinite(e.Position.Y)
                || e.Source is not ("direct" or "voice" or "radio" or "sound") || !actors.Contains(e.ObserverId)
                || e.Classification is not ("anomaly" or "hostile" or "unknown")
                || !double.IsFinite(e.UncertaintyRadius) || e.UncertaintyRadius < 0)
            || snapshot.Noticed.Any(n => n is null || !subjects.TryGetValue(n.SubjectId, out var current) || n.Encounter < 0 || n.Encounter > current)
            || snapshot.Evidence.Select(e => e.SubjectId).Distinct().Count() != snapshot.Evidence.Length
            || snapshot.Noticed.Select(n => n.SubjectId).Distinct().Count() != snapshot.Noticed.Length)
            throw new ArgumentException("Invalid player awareness state.");
        _evidence.Clear(); _noticed.Clear();
        foreach (var evidence in snapshot.Evidence) _evidence.Add(evidence.SubjectId, evidence);
        foreach (var noticed in snapshot.Noticed) _noticed.Add(noticed.SubjectId, noticed.Encounter);
        StopOnDirectSight = snapshot.StopOnDirectSight; StopOnReports = snapshot.StopOnReports;
    }
}
