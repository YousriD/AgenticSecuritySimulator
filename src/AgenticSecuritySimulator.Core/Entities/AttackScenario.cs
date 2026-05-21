namespace AgenticSecuritySimulator.Core.Entities;

public class AttackScenario
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal CriticalityWeight { get; set; } = 1.0m;
    public string DefinitionJson { get; set; } = string.Empty;
}
