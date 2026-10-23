namespace SiteSimulation;

/// <summary>Native-module registry and shared host scheduling. Public mod loading is deferred.</summary>
public sealed class AnomalySystem
{
    private readonly List<AnomalyBody> _bodies = [];
    private readonly List<IAnomalyModule> _modules = [];
    public IReadOnlyList<AnomalyBody> Bodies => _bodies.AsReadOnly();
    public AnomalySystem(IEnumerable<AnomalyDefinition> definitions)
    {
        foreach (var definition in definitions.OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            IAnomalyModule module = definition.Module switch
            {
                "scp:650" => new Scp650Module(definition),
                _ => throw new ArgumentException("Unsupported native module: " + definition.Module)
            };
            if (_bodies.Any(b => b.Id == definition.Id)) throw new ArgumentException("Duplicate anomaly identity.");
            _bodies.Add(new(definition.Id, definition.Module, new(definition.Q, definition.R), poseCount: 6, maxRelocationHexes: 3, observerProfile: definition.ObserverProfile));
            _modules.Add(module);
        }
    }
    internal void RegisterBlockers(FacilityMap map)
    {
        foreach (var body in _bodies)
        {
            if (!map.Walkable(body.Cell)) throw new ArgumentException("Invalid anomaly location: " + body.Id);
            map.DynamicBlockers.Add(body.Cell);
        }
    }
    internal void Evaluate(SimulationWorld world, string cause)
    {
        if (!world.LocalActive) return;
        foreach (var module in _modules)
            module.Evaluate(new AnomalyWorldAccess(world, _bodies.Single(b => b.Id == module.InstanceId)), cause);
    }
    internal AnomalyDiagnostics[] Inspect(SimulationWorld world) => _modules.Select(module =>
        module.Inspect(new AnomalyWorldAccess(world, _bodies.Single(b => b.Id == module.InstanceId)))).ToArray();
    internal bool TrySetFlag(SimulationWorld world, string instanceId, string flag, string actorId, bool enabled, out string reason)
    {
        var module = _modules.SingleOrDefault(m => m.InstanceId == instanceId);
        var actor = world.Actors.SingleOrDefault(a => a.Id == actorId);
        reason = !world.LocalActive ? "local-held" : module is null ? "unknown-instance" : actor is null ? "unknown-actor" : "accepted";
        return reason == "accepted" && module!.TrySetFlag(flag, actorId, actor!.ObserverKind, enabled, out reason);
    }
    internal AnomalyBodySnapshot[] CaptureBodies() => _bodies.Select(b => new AnomalyBodySnapshot(b.Id, b.ModuleId, b.Cell, b.Pose, b.Revision)).ToArray();
    internal ModuleSnapshot[] CaptureModules() => _modules.Select(m => m.Capture()).ToArray();
    internal void Restore(AnomalyBodySnapshot[] bodies, ModuleSnapshot[] modules, SimulationWorld world)
    {
        if (bodies is null || modules is null || bodies.Any(b => b is null) || modules.Any(m => m is null)
            || !bodies.Select(b => b.Id).Order().SequenceEqual(_bodies.Select(b => b.Id).Order())
            || !modules.Select(m => m.InstanceId).Order().SequenceEqual(_modules.Select(m => m.InstanceId).Order()))
            throw new ArgumentException("Invalid anomaly instance set.");
        world.Map.DynamicBlockers.Clear();
        foreach (var snapshot in bodies)
        {
            var body = _bodies.Single(b => b.Id == snapshot.Id);
            if (body.ModuleId != snapshot.ModuleId || (snapshot.Pose < 0 || snapshot.Pose >= body.PoseCount) || snapshot.Revision < 0)
                throw new ArgumentException("Invalid anomaly body state.");
            body.Cell = snapshot.Cell; body.Pose = snapshot.Pose; body.Revision = snapshot.Revision;
        }
        RegisterBlockers(world.Map);
        foreach (var module in _modules) module.Restore(modules.Single(m => m.InstanceId == module.InstanceId), world.LocalMs,
            world.Actors.ToDictionary(a => a.Id, a => a.ObserverKind));
        foreach (var module in _modules)
        {
            var body = _bodies.Single(b => b.Id == module.InstanceId);
            var diagnostics = module.Inspect(new AnomalyWorldAccess(world, body));
            if (diagnostics.Relocations != body.Revision || diagnostics.Observation.Observers.Length != world.Actors.Count)
                throw new ArgumentException("Module and spatial body history disagree.");
        }
    }
}
