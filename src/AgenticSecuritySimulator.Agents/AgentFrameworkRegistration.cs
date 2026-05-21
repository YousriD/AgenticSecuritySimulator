using Microsoft.Extensions.DependencyInjection;

namespace AgenticSecuritySimulator.Agents;

public static class AgentFrameworkRegistration
{
    public static IServiceCollection AddSimulationAgents(this IServiceCollection services, bool useAiPlanners = false)
    {
        services.AddSingleton<AiChatClientFactory>();

        // Register the dynamic wrapper planners. They will automatically load active
        // database connections if configured, otherwise falling back to rule-based systems.
        services.AddSingleton<IRedAgentPlanner, RedAgentPlannerWrapper>();
        services.AddSingleton<IBlueAgentPlanner, BlueAgentPlannerWrapper>();
        services.AddSingleton<INarrativeAgent, NarrativeAgentWrapper>();

        return services;
    }
}
