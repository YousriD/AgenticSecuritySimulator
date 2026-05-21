using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Models;
using AgenticSecuritySimulator.Core.Scenarios;
using System.Text.Json;

namespace AgenticSecuritySimulator.Simulation;

public sealed class SimulationBatchOrchestrator
{
    private readonly SimulationEngine _engine = new();

    public async Task<SimulationBatch> RunBatchAsync(
        Twin twin,
        IReadOnlyList<ScenarioDefinition> scenarios,
        IReadOnlyList<string> selectedScenarioIds,
        SimulationParameters parameters,
        int runCount,
        int seed,
        CancellationToken cancellationToken = default)
    {
        var selected = scenarios
            .Where(s => selectedScenarioIds.Contains(s.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (selected.Count == 0)
            throw new InvalidOperationException("No scenarios selected.");

        var batch = new SimulationBatch
        {
            TwinId = twin.Id,
            RunCount = runCount,
            Seed = seed,
            ParametersJson = JsonSerializer.Serialize(parameters),
            ScenarioIdsJson = JsonSerializer.Serialize(selectedScenarioIds),
            Status = "Running"
        };

        var random = new Random(seed);
        var results = new List<SimulationRunResult>();

        for (var i = 0; i < runCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scenario = selected[i % selected.Count];
            var result = _engine.ExecuteRun(twin, scenario, parameters, i, random);
            results.Add(result);

            if (i % 50 == 0)
                await Task.Yield();
        }

        batch.Runs = results.Select(r => new SimulationRun
        {
            Id = r.RunId,
            BatchId = batch.Id,
            RunIndex = r.RunIndex,
            ScenarioId = r.ScenarioId,
            Status = "Completed",
            ResilienceScore = r.Scores.Composite,
            SubScoresJson = JsonSerializer.Serialize(r.Scores),
            Events = r.Events.Select(e => new SimulationEvent
            {
                RunId = r.RunId,
                Sequence = e.Sequence,
                TimestampOffsetMs = e.TimestampOffsetMs,
                Actor = e.Actor,
                TechniqueId = e.TechniqueId,
                NodeId = e.NodeId,
                Outcome = e.Outcome,
                Message = e.Message
            }).ToList(),
            ScoreDetail = new ResilienceScoreDetail
            {
                RunId = r.RunId,
                Availability = r.Scores.Availability,
                Detection = r.Scores.Detection,
                Containment = r.Scores.Containment,
                Recovery = r.Scores.Recovery,
                BlastRadius = r.Scores.BlastRadius,
                CompositeScore = r.Scores.Composite
            }
        }).ToList();

        var scores = results.Select(r => r.Scores.Composite).OrderBy(x => x).ToList();
        var dimensionFailures = results
            .GroupBy(r => r.Scores.WeakestDimension)
            .OrderByDescending(g => g.Count())
            .First();

        batch.Statistics = new BatchStatistics
        {
            BatchId = batch.Id,
            MeanScore = Math.Round(scores.Average(), 2),
            P10Score = Percentile(scores, 0.10m),
            P90Score = Percentile(scores, 0.90m),
            WeakestDimension = dimensionFailures.Key,
            WeakestDimensionPct = Math.Round(100m * dimensionFailures.Count() / results.Count, 2),
            StatsJson = JsonSerializer.Serialize(new
            {
                Min = scores.First(),
                Max = scores.Last(),
                RunCount = runCount
            })
        };

        batch.Status = "Completed";
        batch.CompletedAtUtc = DateTime.UtcNow;
        return batch;
    }

    private static decimal Percentile(IReadOnlyList<decimal> sorted, decimal percentile)
    {
        if (sorted.Count == 0)
            return 0;
        var index = (int)Math.Floor((sorted.Count - 1) * percentile);
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}
