namespace AgenticSecuritySimulator.Core.Entities;

public class SimulationBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TwinId { get; set; }
    public Twin Twin { get; set; } = null!;
    public int RunCount { get; set; }
    public int Seed { get; set; }
    public string ParametersJson { get; set; } = "{}";
    public string ScenarioIdsJson { get; set; } = "[]";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public ICollection<SimulationRun> Runs { get; set; } = [];
    public BatchStatistics? Statistics { get; set; }
}
