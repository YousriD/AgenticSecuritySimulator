namespace AgenticSecuritySimulator.Core.Models;

public sealed class InfrastructureImportResult
{
    public CsvParseReport Report { get; init; } = new();
    public IReadOnlyList<TwinAssetDto> Assets { get; init; } = [];
    public IReadOnlyList<TwinDependencyDto> Dependencies { get; init; } = [];
    public IReadOnlyList<SecurityControlDto> Controls { get; init; } = [];
    public SimulationParameters SuggestedParameters { get; init; } = new();
}

public sealed class CsvParseReport
{
    public CsvFormat DetectedFormat { get; init; }
    public string? OrganizationHint { get; init; }
    public IReadOnlyDictionary<string, int> RowCountsBySection { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<ColumnMapping> ColumnMappings { get; init; } = [];
    public int AssetCount => RowCountsBySection.Values.Sum();
}

public sealed class ColumnMapping
{
    public string CanonicalField { get; init; } = string.Empty;
    public string SourceColumn { get; init; } = string.Empty;
}

public enum CsvFormat
{
    Unknown,
    Lansweeper,
    DigitalTwinAllInOne,
    Generic
}

public sealed class SecurityControlDto
{
    public string ControlId { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public decimal CoveragePercent { get; set; }
    public string OwnerTeam { get; set; } = string.Empty;
    public string? ComplianceMapping { get; set; }
}
