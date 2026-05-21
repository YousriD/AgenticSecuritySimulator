using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Scenarios;

namespace AgenticSecuritySimulator.Agents;

public interface IRedAgentPlanner
{
    Task<string> PlanAttackPathAsync(Twin twin, ScenarioDefinition scenario, CancellationToken cancellationToken = default);
}

public interface IBlueAgentPlanner
{
    Task<string> PlanResponseAsync(Twin twin, ScenarioDefinition scenario, CancellationToken cancellationToken = default);
}

public interface INarrativeAgent
{
    Task<string> SummarizeBatchAsync(string statsJson, CancellationToken cancellationToken = default);
}
