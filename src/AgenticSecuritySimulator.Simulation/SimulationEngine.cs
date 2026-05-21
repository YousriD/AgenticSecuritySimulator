using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Models;
using AgenticSecuritySimulator.Core.Scenarios;
using AgenticSecuritySimulator.Core.Scoring;

namespace AgenticSecuritySimulator.Simulation;

public sealed class SimulationEngine
{
    public SimulationRunResult ExecuteRun(
        Twin twin,
        ScenarioDefinition scenario,
        SimulationParameters parameters,
        int runIndex,
        Random random)
    {
        var events = new List<SimulatedEvent>();
        var compromised = new HashSet<Guid>();
        var isolatedNodes = new HashSet<Guid>();
        var highValueNodes = twin.Nodes.Where(n => n.CriticalityWeight >= 0.7m).ToList();
        var sequence = 0;
        var timeMs = 0;

        foreach (var step in scenario.Steps)
        {
            // Red targets candidate nodes matching the step target types
            // and avoiding isolated or already compromised nodes (unless it's an impact step)
            var candidates = twin.Nodes
                .Where(n => step.TargetTypes.Contains(n.NodeType, StringComparer.OrdinalIgnoreCase))
                .Where(n => !compromised.Contains(n.Id) || step.Outcome is "impact" or "exfiltration")
                .Where(n => !isolatedNodes.Contains(n.Id))
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = twin.Nodes.Where(n => !isolatedNodes.Contains(n.Id)).ToList();
            }

            if (candidates.Count == 0)
            {
                // Graph is completely locked down / isolated by defender
                break;
            }

            var target = candidates[random.Next(candidates.Count)];

            // Deterministic block logic stochastically driven by EDR Effectiveness slider
            var blocked = step.Actor == "red" && random.NextDouble() < (double)parameters.EdrEffectiveness;

            if (blocked)
            {
                events.Add(new SimulatedEvent
                {
                    Sequence = ++sequence,
                    TimestampOffsetMs = timeMs,
                    Actor = "blue",
                    TechniqueId = step.TechniqueId,
                    NodeId = target.Id,
                    Outcome = "blocked",
                    Message = $"EDR blocked {step.TechniqueId} attack on {target.DisplayName}"
                });
            }
            else
            {
                // Compromise successful
                compromised.Add(target.Id);
                events.Add(new SimulatedEvent
                {
                    Sequence = ++sequence,
                    TimestampOffsetMs = timeMs,
                    Actor = "red",
                    TechniqueId = step.TechniqueId,
                    NodeId = target.Id,
                    Outcome = step.Outcome == "foothold" ? "foothold" : "compromised",
                    Message = $"Red conquered {target.DisplayName} using {step.TechniqueId} ({step.Outcome})"
                });

                // Blue Agent stochastic defense response: stochastically isolate the compromised node!
                // Trigger probability tied directly to the EDR effectiveness slider
                if (random.NextDouble() < (double)parameters.EdrEffectiveness * 0.75)
                {
                    timeMs += random.Next(1, 4) * 60_000; // Containment latency delay

                    isolatedNodes.Add(target.Id);
                    events.Add(new SimulatedEvent
                    {
                        Sequence = ++sequence,
                        TimestampOffsetMs = timeMs,
                        Actor = "blue",
                        TechniqueId = "T1040", // Dynamic Isolation Tactic ID
                        NodeId = target.Id,
                        Outcome = "isolated",
                        Message = $"Blue containment isolated node {target.DisplayName} to secure surrounding zone"
                    });
                }
            }

            timeMs += random.Next(5, 18) * 60_000;
        }

        var compromisedHighValue = highValueNodes.Count(n => compromised.Contains(n.Id) && !isolatedNodes.Contains(n.Id));
        var mttd = (decimal)random.Next(25, 200);
        var mttc = (decimal)random.Next(50, 300);
        var recovery = random.NextDouble() > 0.20;

        var scores = ResilienceCalculator.Calculate(
            parameters,
            compromisedHighValue,
            Math.Max(highValueNodes.Count, 1),
            mttd,
            mttc,
            recovery);

        return new SimulationRunResult
        {
            RunIndex = runIndex,
            ScenarioId = scenario.Id,
            Scores = scores,
            Events = events
        };
    }
}
