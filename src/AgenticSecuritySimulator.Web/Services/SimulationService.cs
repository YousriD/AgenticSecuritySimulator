using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Models;
using AgenticSecuritySimulator.Core.Scenarios;
using AgenticSecuritySimulator.Simulation;
using AgenticSecuritySimulator.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgenticSecuritySimulator.Web.Services;

public sealed class SimulationService(
    AppDbContext db,
    BatchRunService batchRunner,
    IWebHostEnvironment env)
{
    public async Task<IReadOnlyList<ScenarioDefinition>> GetScenariosAsync(CancellationToken ct = default)
    {
        var fromFiles = ScenarioCatalog.LoadFromDirectory(ContentPaths.ScenariosDirectory(env));
        if (fromFiles.Count > 0)
            return fromFiles;

        var fromDb = await db.AttackScenarios.AsNoTracking().ToListAsync(ct);
        return fromDb.Select(s =>
        {
            try
            {
                return JsonSerializer.Deserialize<ScenarioDefinition>(s.DefinitionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            catch
            {
                return new ScenarioDefinition
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CriticalityWeight = s.CriticalityWeight,
                    Steps = []
                };
            }
        }).Where(s => s.Steps.Count > 0).ToList();
    }

    public Task<Guid?> GetLatestTwinIdAsync(CancellationToken ct = default) =>
        db.Twins
            .OrderByDescending(t => t.ImportedAtUtc)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

    public Task<SimulationBatch> RunBatchAsync(
        Guid twinId,
        IReadOnlyList<string> scenarioIds,
        SimulationParameters parameters,
        int runCount,
        int seed,
        IProgress<string>? progress = null,
        IProgress<SimulationRunResult>? runProgress = null,
        CancellationToken ct = default) =>
        batchRunner.RunAsync(twinId, scenarioIds, parameters, runCount, seed, progress, runProgress, ct);

    public Task<SimulationBatch?> GetBatchAsync(Guid batchId, CancellationToken ct = default) =>
        db.SimulationBatches
            .Include(b => b.Statistics)
            .Include(b => b.Runs)
            .ThenInclude(r => r.Events)
            .Include(b => b.Runs)
            .ThenInclude(r => r.ScoreDetail)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);

    public Task<Twin?> GetTwinAsync(Guid twinId, CancellationToken ct = default) =>
        db.Twins
            .Include(t => t.Nodes)
            .Include(t => t.Edges)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == twinId, ct);
}
