using Godot;
using SiteSimulation;

namespace SitePresentation;

/// <summary>One scheduler shared by standalone and attached modes. Views never advance the simulation themselves.</summary>
public sealed class SimulationRuntime(SpatialSimulation simulation)
{
    public SpatialSimulation Simulation { get; } = simulation;
    private double _accumulator;
    public void Advance(double delta)
    {
        _accumulator += delta * Simulation.Speed;
        while (_accumulator >= SpatialSimulation.TickMs / 1000.0)
        { Simulation.Tick(); _accumulator -= SpatialSimulation.TickMs / 1000.0; }
    }
    public void ResetAccumulator() => _accumulator = 0;
    public static SimulationRuntime Load(string operation = "scp650-operation.json") => new(new(
        Godot.FileAccess.GetFileAsString("res://Content/spatial-facility.json"), Godot.FileAccess.GetFileAsString("res://Content/" + operation)));
}
