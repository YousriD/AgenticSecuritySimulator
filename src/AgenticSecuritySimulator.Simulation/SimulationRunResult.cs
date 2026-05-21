using AgenticSecuritySimulator.Core.Models;

namespace AgenticSecuritySimulator.Simulation;

public sealed class SimulationRunResult
{
    public Guid RunId { get; init; } = Guid.NewGuid();
    public int RunIndex { get; init; }
    public string ScenarioId { get; init; } = string.Empty;
    public ResilienceSubScores Scores { get; init; } = null!;
    public IReadOnlyList<SimulatedEvent> Events { get; init; } = [];
}

public sealed class SimulatedEvent
{
    public int Sequence { get; init; }
    public int TimestampOffsetMs { get; init; }
    public string Actor { get; init; } = string.Empty;
    public string? TechniqueId { get; init; }
    public Guid? NodeId { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
