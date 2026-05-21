namespace AgenticSecuritySimulator.Core.Entities;

public class BatchStatistics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public SimulationBatch Batch { get; set; } = null!;
    public decimal MeanScore { get; set; }
    public decimal P10Score { get; set; }
    public decimal P90Score { get; set; }
    public string WeakestDimension { get; set; } = string.Empty;
    public decimal WeakestDimensionPct { get; set; }
    public string StatsJson { get; set; } = "{}";
}
