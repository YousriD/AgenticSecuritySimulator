namespace AgenticSecuritySimulator.Core.Models;

public sealed class TwinImportRequest
{
    public string OrganizationName { get; set; } = "Contoso";
    public string TwinName { get; set; } = "Imported Twin";
    public IReadOnlyList<TwinAssetDto> Assets { get; set; } = [];
    public IReadOnlyList<TwinDependencyDto> Dependencies { get; set; } = [];
    public IReadOnlyList<SecurityControlDto> Controls { get; set; } = [];
    public SimulationParameters? SimulationParameters { get; set; }
}

public sealed class TwinAssetDto
{
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Fqdn { get; set; }
    public string? Ou { get; set; }
    public string? Site { get; set; }
    public string? Os { get; set; }
    public string? Department { get; set; }
    public string? CriticalityTag { get; set; }
    public string? Role { get; set; }
    public string? Environment { get; set; }
    public string? Vlan { get; set; }
    public bool? EdrInstalled { get; set; }
    public int? PatchLevelDays { get; set; }
    public bool? BackupEnabled { get; set; }
    public string? OwnerEmail { get; set; }
    public string? Vendor { get; set; }
    public string? Status { get; set; }
    public string SourceSection { get; set; } = string.Empty;
}

public sealed class TwinDependencyDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Kind { get; set; } = "network";
    public bool Encrypted { get; set; }
    public string? LinkType { get; set; }
}
