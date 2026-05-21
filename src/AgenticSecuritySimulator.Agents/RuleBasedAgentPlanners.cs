using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Scenarios;
using Microsoft.Extensions.AI;

namespace AgenticSecuritySimulator.Agents;

/// <summary>
/// Rule-based fallback planners — $0 LLM cost.
/// </summary>
public sealed class RuleBasedRedAgentPlanner : IRedAgentPlanner
{
    public Task<string> PlanAttackPathAsync(Twin twin, ScenarioDefinition scenario, CancellationToken cancellationToken = default)
    {
        var entry = twin.Nodes
            .OrderByDescending(n => n.CriticalityWeight)
            .FirstOrDefault(n => scenario.Steps.FirstOrDefault()?.TargetTypes.Contains(n.NodeType) == true)
            ?? twin.Nodes.FirstOrDefault()
            ?? new TwinNode { DisplayName = "Unknown" };

        var plan = $"Prioritize {scenario.Name}: establish foothold on {entry.DisplayName}, follow scripted MITRE chain.";
        return Task.FromResult(plan);
    }
}

public sealed class RuleBasedBlueAgentPlanner : IBlueAgentPlanner
{
    public Task<string> PlanResponseAsync(Twin twin, ScenarioDefinition scenario, CancellationToken cancellationToken = default)
    {
        var plan = $"Execute containment playbooks for {scenario.Id}; isolate affected zones; restore from backup per RPO.";
        return Task.FromResult(plan);
    }
}

public sealed class RuleBasedNarrativeAgent : INarrativeAgent
{
    public Task<string> SummarizeBatchAsync(string statsJson, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            "Monte Carlo simulation completed. Review resilience distribution and weakest dimension before board briefing.");
    }
}

/// <summary>
/// Hybrid wrapper planners executing AI prompts via dynamic IChatClient with rule-based fallback.
/// </summary>
public sealed class RedAgentPlannerWrapper : IRedAgentPlanner
{
    private readonly AiChatClientFactory _factory;
    private readonly RuleBasedRedAgentPlanner _fallback = new();

    public RedAgentPlannerWrapper(AiChatClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> PlanAttackPathAsync(Twin twin, ScenarioDefinition scenario, CancellationToken cancellationToken = default)
    {
        var client = await _factory.GetActiveClientAsync(cancellationToken);
        if (client is null)
            return await _fallback.PlanAttackPathAsync(twin, scenario, cancellationToken);

        var prompt = $"Analyze the network topology of organization: '{twin.Name}' with {twin.Nodes.Count} nodes.\n" +
                     $"Formulate a high-level attacker strategy for scenario: '{scenario.Name}' ({scenario.Description}).\n" +
                     $"Identify potential targets, lateral movement steps, and compromised goals.\n" +
                     $"Write a concise 2-sentence threat strategy.";

        try
        {
            var completion = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, prompt) }, null, cancellationToken);
            return completion.Text ?? await _fallback.PlanAttackPathAsync(twin, scenario, cancellationToken);
        }
        catch
        {
            return await _fallback.PlanAttackPathAsync(twin, scenario, cancellationToken);
        }
    }
}

public sealed class BlueAgentPlannerWrapper : IBlueAgentPlanner
{
    private readonly AiChatClientFactory _factory;
    private readonly RuleBasedBlueAgentPlanner _fallback = new();

    public BlueAgentPlannerWrapper(AiChatClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> PlanResponseAsync(Twin twin, ScenarioDefinition scenario, CancellationToken cancellationToken = default)
    {
        var client = await _factory.GetActiveClientAsync(cancellationToken);
        if (client is null)
            return await _fallback.PlanResponseAsync(twin, scenario, cancellationToken);

        var prompt = $"Analyze active security incident: '{scenario.Name}' on infrastructure '{twin.Name}'.\n" +
                     $"As the lead Blue Agent, outline containment and recovery playbooks to deploy.\n" +
                     $"Write a concise 2-sentence defensive strategy.";

        try
        {
            var completion = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, prompt) }, null, cancellationToken);
            return completion.Text ?? await _fallback.PlanResponseAsync(twin, scenario, cancellationToken);
        }
        catch
        {
            return await _fallback.PlanResponseAsync(twin, scenario, cancellationToken);
        }
    }
}

public sealed class NarrativeAgentWrapper : INarrativeAgent
{
    private readonly AiChatClientFactory _factory;
    private readonly RuleBasedNarrativeAgent _fallback = new();

    public NarrativeAgentWrapper(AiChatClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> SummarizeBatchAsync(string statsJson, CancellationToken cancellationToken = default)
    {
        var client = await _factory.GetActiveClientAsync(cancellationToken);
        if (client is null)
            return await _fallback.SummarizeBatchAsync(statsJson, cancellationToken);

        var prompt = $"You are a CISO advisor analyzing simulation batch statistics:\n{statsJson}\n" +
                     $"Draft a professional executive summary paragraph for the Board of Directors explaining the overall resilience, weakest links, and high-priority recommendations.";

        try
        {
            var completion = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, prompt) }, null, cancellationToken);
            return completion.Text ?? await _fallback.SummarizeBatchAsync(statsJson, cancellationToken);
        }
        catch
        {
            return await _fallback.SummarizeBatchAsync(statsJson, cancellationToken);
        }
    }
}
