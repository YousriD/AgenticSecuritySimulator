namespace AgenticSecuritySimulator.Core.Entities;

public class SimulationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public SimulationBatch Batch { get; set; } = null!;
    public int RunIndex { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal? ResilienceScore { get; set; }
    public string? SubScoresJson { get; set; }
    public ICollection<SimulationEvent> Events { get; set; } = [];
    public ResilienceScoreDetail? ScoreDetail { get; set; }
}
