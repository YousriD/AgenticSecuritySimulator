namespace AgenticSecuritySimulator.Core.Models;

public sealed class SimulationParameters
{
    public decimal EdrEffectiveness { get; set; } = 0.6m;
    public int BackupRpoHours { get; set; } = 24;
    public int PatchLagDays { get; set; } = 30;
    public decimal MttdTargetMinutes { get; set; } = 60;
    public decimal MttcTargetMinutes { get; set; } = 120;
}
