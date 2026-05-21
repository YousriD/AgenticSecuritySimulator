using AgenticSecuritySimulator.Core.Models;

namespace AgenticSecuritySimulator.Web.Services;

public sealed class AppState
{
    public Guid? CurrentTwinId { get; set; }
    public Guid? LastBatchId { get; set; }
    public CsvParseReport? LastImportReport { get; set; }
    public SimulationParameters SuggestedParameters { get; set; } = new();
}
