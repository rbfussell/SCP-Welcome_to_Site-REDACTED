namespace SiteSimulation;

/// <summary>All SCP-specific continuation state; physical location lives in AnomalyBody.</summary>
internal sealed class Scp650State
{
    public uint RandomState { get; set; }
    public long NextEvaluationMs { get; set; }
    public int Relocations { get; set; }
    public int LastCandidateCount { get; set; }
    public string[] LastObserverIds { get; set; } = [];
    public string LastReason { get; set; } = "ready";
    public Dictionary<string, int> LastWitnessEncounter { get; set; } = [];
}
