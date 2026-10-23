using System.Text.Json;

namespace SiteSimulation;

/// <summary>SCP-650 rules only: no renderer, pathfinding, mission trigger or attack AI.</summary>
internal sealed class Scp650Module : IAnomalyModule
{
    private const int RetryMs = 500;
    private const int CooldownMs = 2000;
    private readonly string _profile;
    private Scp650State _state;
    public string InstanceId { get; }
    public string ModuleId => "scp:650";
    public Scp650Module(AnomalyDefinition definition)
    {
        if (!ObservationService.ValidProfile(definition.ObserverProfile) || definition.Seed == 0)
            throw new ArgumentException("SCP-650 requires a known observer profile and nonzero seed.");
        InstanceId = definition.Id; _profile = definition.ObserverProfile;
        _state = new() { RandomState = definition.Seed };
    }
    private uint NextRandom()
    {
        var x = _state.RandomState;
        x ^= x << 13; x ^= x >> 17; x ^= x << 5;
        return _state.RandomState = x;
    }
    public void Evaluate(IAnomalyWorld world, string cause)
    {
        var report = world.Observation(_profile);
        var observers = report.EligibleIds;
        if (!_state.LastObserverIds.SequenceEqual(observers))
        {
            world.Trace(observers.Length == 0 ? "observation-lost" : "observation-acquired",
                $"{cause}; eligible [{string.Join(",", observers)}]; profile {_profile}");
            _state.LastObserverIds = observers;
        }
        if (_state.Relocations > 0)
        foreach (var witness in report.Observers.Where(o => o.Detected && o.Kind == "human"))
        {
            if (_state.LastWitnessEncounter.GetValueOrDefault(witness.ActorId) == _state.Relocations) continue;
            _state.LastWitnessEncounter[witness.ActorId] = _state.Relocations;
            world.Witnessed(witness.ActorId, _state.Relocations);
        }
        if (report.Observed) { _state.LastReason = "observed; self-relocation suppressed"; return; }
        if (world.LocalMs < _state.NextEvaluationMs) return;
        // Complacency is explicitly authored for each person. Discovery alone never enables it.
        // Threat poses appear close behind; relaxed poses use a wider, non-rear-only area.
        var candidates = report.Observers.Where(o => o.Kind == "human")
            .SelectMany(o => world.Candidates(_profile, Placement(o.ActorId)))
            .OrderBy(c => c.Cell.Q).ThenBy(c => c.Cell.R).ThenBy(c => c.NearActorId, StringComparer.Ordinal).ToArray();
        _state.LastCandidateCount = candidates.Length;
        if (candidates.Length == 0)
        {
            _state.LastReason = "no valid unobserved destination";
            _state.NextEvaluationMs = world.LocalMs + RetryMs;
            world.Trace("retry-scheduled", $"{cause}; no candidate; retry at local {_state.NextEvaluationMs}ms");
            return;
        }
        var beforeRandom = _state.RandomState;
        // Choose a person, then a position: more available cells do not bias the target toward that person.
        var targets = candidates.Select(c => c.NearActorId).Distinct().Order(StringComparer.Ordinal).ToArray();
        var target = targets[(int)(NextRandom() % (uint)targets.Length)];
        var positions = candidates.Where(c => c.NearActorId == target).ToArray();
        var choice = positions[(int)(NextRandom() % (uint)positions.Length)];
        var complacent = _state.ComplacentActorIds.Contains(choice.NearActorId);
        var pose = complacent ? 3 + (int)(NextRandom() % 3u) : 1 + (int)(NextRandom() % 2u);
        if (!world.TryRelocate(world.Body.Revision, choice.Cell, pose, _profile, Placement(choice.NearActorId), out var reason))
        {
            _state.RandomState = beforeRandom;
            _state.LastReason = "commit rejected: " + reason;
            _state.NextEvaluationMs = world.LocalMs + RetryMs;
            world.Trace("relocation-rejected", reason);
            return;
        }
        _state.Relocations++;
        _state.LastReason = $"unobserved; relocated near {choice.NearActorId}";
        _state.NextEvaluationMs = world.LocalMs + CooldownMs;
        world.Trace("relocation-selected", $"{candidates.Length} valid candidates; {(complacent ? "relaxed" : "threat")} profile near {target}; {choice.DistanceHexes} hexes; encounter {_state.Relocations}; next evaluation {_state.NextEvaluationMs}ms");
    }
    private RelocationQuery Placement(string actorId) => _state.ComplacentActorIds.Contains(actorId)
        ? new(2, 3, false, "human", actorId, RequireClearLine: true)
        : new(1, 1, true, "human", actorId, RequireClearLine: true);

    public bool TrySetFlag(string flag, string actorId, string actorKind, bool enabled, out string reason)
    {
        reason = flag != "complacency" ? "unknown-module-flag" : actorKind != "human" ? "requires-human-target" : "accepted";
        if (reason != "accepted") return false;
        if (enabled) _state.ComplacentActorIds.Add(actorId); else _state.ComplacentActorIds.Remove(actorId);
        return true;
    }
    public AnomalyDiagnostics Inspect(IAnomalyWorld world)
    {
        var report = world.Observation(_profile);
        return new(InstanceId, report.Observed ? "OBSERVED / frozen" : _state.LastReason, world.Body.Pose,
            _state.Relocations, _state.LastCandidateCount, _state.NextEvaluationMs, report,
            _state.LastWitnessEncounter.Where(p => p.Value == _state.Relocations).Select(p => p.Key).Order().ToArray(),
            _state.ComplacentActorIds.Order(StringComparer.Ordinal).Select(id => new ModuleFlag("complacency", id, true)).ToArray());
    }
    public ModuleSnapshot Capture() => new(InstanceId, ModuleId, 2, JsonSerializer.Serialize(_state));
    public void Restore(ModuleSnapshot snapshot, long localMs, IReadOnlyDictionary<string, string> actorKinds)
    {
        if (snapshot.Version != 2) throw new ArgumentException("SCP-650 save requires module schema 2 (explicit complacency); earlier prototype saves are not migrated.");
        var state = JsonSerializer.Deserialize<Scp650State>(snapshot.Json) ?? throw new ArgumentException("Empty SCP state.");
        if (snapshot.InstanceId != InstanceId || snapshot.ModuleId != ModuleId || snapshot.Version != 2 || state.RandomState == 0
            || state.Relocations < 0 || state.LastCandidateCount < 0 || state.LastCandidateCount > 16384
            || state.NextEvaluationMs < 0 || state.NextEvaluationMs > localMs + CooldownMs
            || state.LastObserverIds is null || state.LastWitnessEncounter is null || state.LastReason is null || state.ComplacentActorIds is null
            || state.ComplacentActorIds.Any(id => !actorKinds.TryGetValue(id, out var kind) || kind != "human")
            || state.LastObserverIds.Any(id => !actorKinds.ContainsKey(id))
            || state.LastWitnessEncounter.Any(p => !actorKinds.ContainsKey(p.Key) || p.Value < 1 || p.Value > state.Relocations))
            throw new ArgumentException("Invalid SCP-650 continuation state.");
        _state = state;
    }
}
