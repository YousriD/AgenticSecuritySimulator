namespace AgenticSecuritySimulator.Core.Scenarios;

public sealed class ScenarioDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal CriticalityWeight { get; set; } = 1.0m;
    public IReadOnlyList<string> MitreTechniques { get; set; } = [];
    public IReadOnlyList<ScenarioStep> Steps { get; set; } = [];
}

public sealed class ScenarioStep
{
    public string TechniqueId { get; set; } = string.Empty;
    public string Actor { get; set; } = "red";
    public IReadOnlyList<string> TargetTypes { get; set; } = [];
    public string Outcome { get; set; } = string.Empty;
}
