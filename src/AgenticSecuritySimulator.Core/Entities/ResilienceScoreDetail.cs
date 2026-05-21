namespace AgenticSecuritySimulator.Core.Entities;

public class ResilienceScoreDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RunId { get; set; }
    public SimulationRun Run { get; set; } = null!;
    public decimal Availability { get; set; }
    public decimal Detection { get; set; }
    public decimal Containment { get; set; }
    public decimal Recovery { get; set; }
    public decimal BlastRadius { get; set; }
    public decimal CompositeScore { get; set; }
}
