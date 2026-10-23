using System.Text.Json;

namespace SiteSimulation;

/// <summary>SCP-650 rules only: no renderer, pathfinding, mission trigger or attack AI.</summary>
internal sealed class Scp650Module : IAnomalyModule
{
    private static readonly RelocationQuery Placement = new(1, 3, true, "human");
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
        var candidates = world.Candidates(_profile, Placement);
        _state.LastCandidateCount = candidates.Count;
        if (candidates.Count == 0)
        {
            _state.LastReason = "no valid unobserved destination";
            _state.NextEvaluationMs = world.LocalMs + RetryMs;
            world.Trace("retry-scheduled", $"{cause}; no candidate; retry at local {_state.NextEvaluationMs}ms");
            return;
        }
        var beforeRandom = _state.RandomState;
        var choice = candidates[(int)(NextRandom() % (uint)candidates.Count)];
        // Unfamiliar personnel receive a threatening pose; familiarity permits variants.
        var familiar = _state.LastWitnessEncounter.ContainsKey(choice.NearActorId);
        var pose = 1 + (int)(NextRandom() % (familiar ? 5u : 2u));
        if (!world.TryRelocate(world.Body.Revision, choice.Cell, pose, _profile, Placement, out var reason))
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
        world.Trace("relocation-selected", $"{candidates.Count} valid candidates; {(familiar ? "familiar" : "unfamiliar")} witness; encounter {_state.Relocations}; next evaluation {_state.NextEvaluationMs}ms");
    }
    public AnomalyDiagnostics Inspect(IAnomalyWorld world)
    {
        var report = world.Observation(_profile);
        return new(InstanceId, report.Observed ? "OBSERVED / frozen" : _state.LastReason, world.Body.Pose,
            _state.Relocations, _state.LastCandidateCount, _state.NextEvaluationMs, report,
            _state.LastWitnessEncounter.Where(p => p.Value == _state.Relocations).Select(p => p.Key).Order().ToArray());
    }
    public ModuleSnapshot Capture() => new(InstanceId, ModuleId, 1, JsonSerializer.Serialize(_state));
    public void Restore(ModuleSnapshot snapshot, long localMs, IReadOnlySet<string> actorIds)
    {
        var state = JsonSerializer.Deserialize<Scp650State>(snapshot.Json) ?? throw new ArgumentException("Empty SCP state.");
        if (snapshot.InstanceId != InstanceId || snapshot.ModuleId != ModuleId || snapshot.Version != 1 || state.RandomState == 0
            || state.Relocations < 0 || state.LastCandidateCount < 0 || state.LastCandidateCount > 16384
            || state.NextEvaluationMs < 0 || state.NextEvaluationMs > localMs + CooldownMs
            || state.LastObserverIds is null || state.LastWitnessEncounter is null || state.LastReason is null
            || state.LastObserverIds.Any(id => !actorIds.Contains(id))
            || state.LastWitnessEncounter.Any(p => !actorIds.Contains(p.Key) || p.Value < 1 || p.Value > state.Relocations))
            throw new ArgumentException("Invalid SCP-650 continuation state.");
        _state = state;
    }
}
