namespace AgenticSecuritySimulator.Core.Entities;

public class SimulationEvent
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public SimulationRun Run { get; set; } = null!;
    public int Sequence { get; set; }
    public int TimestampOffsetMs { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string? TechniqueId { get; set; }
    public Guid? NodeId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
