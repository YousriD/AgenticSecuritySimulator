using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Models;
using AgenticSecuritySimulator.Core.Scenarios;
using AgenticSecuritySimulator.Simulation;
using AgenticSecuritySimulator.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgenticSecuritySimulator.Web.Services;

public sealed class BatchRunService(
    IServiceScopeFactory scopeFactory,
    SimulationBatchOrchestrator orchestrator,
    IWebHostEnvironment env,
    ILogger<BatchRunService> logger)
{
    public async Task<SimulationBatch> RunAsync(
        Guid twinId,
        IReadOnlyList<string> scenarioIds,
        SimulationParameters parameters,
        int runCount,
        int seed,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report("Loading twin…");
        Twin twin;
        IReadOnlyList<ScenarioDefinition> scenarios;

        await using (var loadScope = scopeFactory.CreateAsyncScope())
        {
            var loadDb = loadScope.ServiceProvider.GetRequiredService<AppDbContext>();
            twin = await loadDb.Twins
                .AsNoTracking()
                .Include(t => t.Nodes)
                .Include(t => t.Edges)
                .FirstOrDefaultAsync(t => t.Id == twinId, ct)
                ?? throw new InvalidOperationException("Twin not found. Import a twin first.");

            scenarios = await LoadScenariosAsync(loadDb, ct);
        }

        if (scenarios.Count == 0)
            throw new InvalidOperationException("No attack scenarios available.");

        progress?.Report($"Running {runCount} simulations…");
        logger.LogInformation("Starting batch: twin={TwinId}, runs={RunCount}", twinId, runCount);

        var computed = await Task.Run(
            () => orchestrator.RunBatchAsync(twin, scenarios, scenarioIds, parameters, runCount, seed, ct),
            ct);

        progress?.Report("Saving results…");
        await using var saveScope = scopeFactory.CreateAsyncScope();
        var saveDb = saveScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await PersistAsync(saveDb, computed, runCount, ct);

        logger.LogInformation("Batch saved: {BatchId}, mean R={Mean}", saved.Id, saved.Statistics?.MeanScore);
        progress?.Report("Done.");
        return saved;
    }

    private async Task<IReadOnlyList<ScenarioDefinition>> LoadScenariosAsync(AppDbContext db, CancellationToken ct)
    {
        var fromFiles = ScenarioCatalog.LoadFromDirectory(ContentPaths.ScenariosDirectory(env));
        if (fromFiles.Count > 0)
            return fromFiles;

        var fromDb = await db.AttackScenarios.AsNoTracking().ToListAsync(ct);
        return fromDb.Select(s => JsonSerializer.Deserialize<ScenarioDefinition>(s.DefinitionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!)
            .Where(s => s.Steps.Count > 0)
            .ToList();
    }

    private static async Task<SimulationBatch> PersistAsync(
        AppDbContext db,
        SimulationBatch computed,
        int runCount,
        CancellationToken ct)
    {
        var scores = computed.Runs.Select(r => r.ResilienceScore ?? 0).OrderBy(x => x).ToList();
        if (scores.Count == 0)
            throw new InvalidOperationException("Simulation produced no scores.");

        var median = computed.Runs
            .Where(r => r.ResilienceScore.HasValue)
            .OrderBy(r => r.ResilienceScore)
            .ElementAt(scores.Count / 2);

        var stats = computed.Statistics ?? throw new InvalidOperationException("Batch statistics missing.");
        stats.StatsJson = JsonSerializer.Serialize(new
        {
            Min = scores[0],
            Max = scores[^1],
            RunCount = runCount,
            Scores = scores
        });

        var batch = new SimulationBatch
        {
            Id = computed.Id,
            TwinId = computed.TwinId,
            RunCount = runCount,
            Seed = computed.Seed,
            ParametersJson = computed.ParametersJson,
            ScenarioIdsJson = computed.ScenarioIdsJson,
            Status = computed.Status,
            CreatedAtUtc = computed.CreatedAtUtc,
            CompletedAtUtc = computed.CompletedAtUtc
        };

        var run = new SimulationRun
        {
            Id = median.Id,
            BatchId = batch.Id,
            RunIndex = median.RunIndex,
            ScenarioId = median.ScenarioId,
            Status = median.Status,
            ResilienceScore = median.ResilienceScore,
            SubScoresJson = median.SubScoresJson
        };

        var events = median.Events.Select(e => new SimulationEvent
        {
            RunId = run.Id,
            Sequence = e.Sequence,
            TimestampOffsetMs = e.TimestampOffsetMs,
            Actor = e.Actor,
            TechniqueId = e.TechniqueId,
            NodeId = e.NodeId,
            Outcome = e.Outcome,
            Message = e.Message
        }).ToList();

        var scoreDetail = median.ScoreDetail is null ? null : new ResilienceScoreDetail
        {
            RunId = run.Id,
            Availability = median.ScoreDetail.Availability,
            Detection = median.ScoreDetail.Detection,
            Containment = median.ScoreDetail.Containment,
            Recovery = median.ScoreDetail.Recovery,
            BlastRadius = median.ScoreDetail.BlastRadius,
            CompositeScore = median.ScoreDetail.CompositeScore
        };

        var statistics = new BatchStatistics
        {
            Id = Guid.NewGuid(),
            BatchId = batch.Id,
            MeanScore = stats.MeanScore,
            P10Score = stats.P10Score,
            P90Score = stats.P90Score,
            WeakestDimension = stats.WeakestDimension,
            WeakestDimensionPct = stats.WeakestDimensionPct,
            StatsJson = stats.StatsJson
        };

        db.SimulationBatches.Add(batch);
        db.SimulationRuns.Add(run);
        db.SimulationEvents.AddRange(events);
        if (scoreDetail is not null)
            db.ResilienceScores.Add(scoreDetail);
        db.BatchStatistics.Add(statistics);

        await db.SaveChangesAsync(ct);

        batch.Statistics = statistics;
        batch.Runs = [run];
        return batch;
    }
}
