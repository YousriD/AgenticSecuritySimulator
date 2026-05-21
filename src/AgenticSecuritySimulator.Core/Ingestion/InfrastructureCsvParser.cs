using AgenticSecuritySimulator.Core.Models;

namespace AgenticSecuritySimulator.Core.Ingestion;

public static class InfrastructureCsvParser
{
    public static InfrastructureImportResult Parse(string csvContent)
    {
        var rows = CsvReader.ReadAllRows(csvContent);
        if (rows.Count == 0)
            throw new InvalidOperationException("CSV is empty.");

        var headers = rows[0];
        var columnMap = CsvColumnMapper.MapHeaders(headers);
        var format = DetectFormat(headers, rows);

        return format switch
        {
            CsvFormat.DigitalTwinAllInOne => ParseAllInOne(rows, columnMap),
            CsvFormat.Lansweeper => ParseLansweeper(rows, columnMap),
            _ => ParseGeneric(rows, columnMap)
        };
    }

    public static InfrastructureImportResult Analyze(string csvContent) => Parse(csvContent);

    private static CsvFormat DetectFormat(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var normalized = headers.Select(h => h.Trim().ToLowerInvariant()).ToHashSet();
        if (normalized.Contains("dataset_type"))
            return CsvFormat.DigitalTwinAllInOne;
        if (normalized.Contains("assetname") && normalized.Contains("assettype"))
            return CsvFormat.Lansweeper;
        if (normalized.Contains("hostname") || normalized.Contains("device_type"))
            return CsvFormat.Generic;
        return CsvFormat.Unknown;
    }

    private static InfrastructureImportResult ParseAllInOne(
        IReadOnlyList<IReadOnlyList<string>> rows,
        Dictionary<string, string> columnMap)
    {
        var warnings = new List<string>();
        var rowCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var assets = new List<TwinAssetDto>();
        var dependencies = new List<TwinDependencyDto>();
        var controls = new List<SecurityControlDto>();
        var nodeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? orgHint = null;

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            var map = RowDictionary(headers: rows[0], row, columnMap);
            var section = Get(map, "dataset_type").ToLowerInvariant();
            if (string.IsNullOrEmpty(section))
                section = "unknown";

            rowCounts[section] = rowCounts.GetValueOrDefault(section) + 1;

            switch (section)
            {
                case "devices":
                case "servers":
                case "network_devices":
                    var asset = MapAsset(map, section);
                    if (!nodeKeys.Add(asset.AssetName))
                        warnings.Add($"Duplicate asset key skipped: {asset.AssetName}");
                    else
                        assets.Add(asset);
                    break;

                case "network_links":
                    var from = ResolveNodeKey(map, "source_node");
                    var to = ResolveNodeKey(map, "destination_node");
                    if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                    {
                        warnings.Add($"Link row {i + 1} missing source or destination.");
                        break;
                    }

                    dependencies.Add(new TwinDependencyDto
                    {
                        From = from,
                        To = to,
                        Kind = MapLinkKind(Get(map, "link_type")),
                        Encrypted = ParseBool(Get(map, "encrypted")) == true,
                        LinkType = NullIfEmpty(Get(map, "link_type"))
                    });
                    break;

                case "security_controls":
                    controls.Add(new SecurityControlDto
                    {
                        ControlId = Get(map, "control_id", "primary_id"),
                        ControlType = Get(map, "control_type"),
                        Platform = Get(map, "platform"),
                        CoveragePercent = ParseDecimal(Get(map, "coverage_percent")) ?? 0,
                        OwnerTeam = Get(map, "owner_team"),
                        ComplianceMapping = NullIfEmpty(Get(map, "compliance_mapping"))
                    });
                    break;
            }

            orgHint ??= InferOrgFromEmail(Get(map, "owner_email"));
        }

        var unresolvedLinks = dependencies.Count(d =>
            !nodeKeys.Contains(d.From) || !nodeKeys.Contains(d.To));
        if (unresolvedLinks > 0)
            warnings.Add($"{unresolvedLinks} links reference hostnames not found as assets (network links often use short names like CORE-03).");

        var inferred = TopologyInference.InferAdditionalEdges(assets, dependencies, controls);
        dependencies.AddRange(inferred);

        return new InfrastructureImportResult
        {
            Report = new CsvParseReport
            {
                DetectedFormat = CsvFormat.DigitalTwinAllInOne,
                OrganizationHint = orgHint ?? "corp-example.local",
                RowCountsBySection = rowCounts,
                Warnings = warnings,
                ColumnMappings = CsvColumnMapper.DescribeMappings(columnMap)
            },
            Assets = assets,
            Dependencies = dependencies,
            Controls = controls,
            SuggestedParameters = DeriveSimulationParameters(assets, controls)
        };
    }

    private static InfrastructureImportResult ParseLansweeper(
        IReadOnlyList<IReadOnlyList<string>> rows,
        Dictionary<string, string> columnMap)
    {
        var assets = LansweeperCsvParser.ParseRows(rows, columnMap);
        return new InfrastructureImportResult
        {
            Report = new CsvParseReport
            {
                DetectedFormat = CsvFormat.Lansweeper,
                RowCountsBySection = new Dictionary<string, int> { ["assets"] = assets.Count },
                ColumnMappings = CsvColumnMapper.DescribeMappings(columnMap)
            },
            Assets = assets,
            Dependencies = TopologyInference.InferAdditionalEdges(assets, [], []),
            SuggestedParameters = DeriveSimulationParameters(assets, [])
        };
    }

    private static InfrastructureImportResult ParseGeneric(
        IReadOnlyList<IReadOnlyList<string>> rows,
        Dictionary<string, string> columnMap)
    {
        var assets = new List<TwinAssetDto>();
        for (var i = 1; i < rows.Count; i++)
        {
            var map = RowDictionary(rows[0], rows[i], columnMap);
            if (map.Count == 0) continue;
            assets.Add(MapAsset(map, "assets"));
        }

        return new InfrastructureImportResult
        {
            Report = new CsvParseReport
            {
                DetectedFormat = CsvFormat.Generic,
                RowCountsBySection = new Dictionary<string, int> { ["assets"] = assets.Count },
                ColumnMappings = CsvColumnMapper.DescribeMappings(columnMap)
            },
            Assets = assets,
            Dependencies = TopologyInference.InferAdditionalEdges(assets, [], []),
            SuggestedParameters = DeriveSimulationParameters(assets, [])
        };
    }

    private static TwinAssetDto MapAsset(IReadOnlyDictionary<string, string> map, string section)
    {
        var name = ResolveNodeKey(map, "asset_name", "primary_id");
        var deviceType = Get(map, "device_type");
        if (string.IsNullOrEmpty(deviceType))
            deviceType = Get(map, "asset_type");
        if (string.IsNullOrEmpty(deviceType))
            deviceType = section switch
            {
                "servers" => "Server",
                "network_devices" => "Network",
                _ => "Workstation"
            };

        return new TwinAssetDto
        {
            AssetName = name,
            AssetType = NormalizeAssetType(deviceType, section),
            IpAddress = NullIfEmpty(Get(map, "ip_address")),
            Fqdn = NullIfEmpty(Get(map, "fqdn")),
            Ou = NullIfEmpty(Get(map, "ou")),
            Site = NullIfEmpty(Get(map, "site")),
            Os = NullIfEmpty(Get(map, "os")),
            Department = NullIfEmpty(Get(map, "department")),
            CriticalityTag = NullIfEmpty(Get(map, "criticality")),
            Role = NullIfEmpty(Get(map, "role")) ??
                     (section == "network_devices" ? NullIfEmpty(deviceType) : null),
            Environment = NullIfEmpty(Get(map, "environment")),
            Vlan = NullIfEmpty(Get(map, "vlan")),
            EdrInstalled = ParseBool(Get(map, "edr_installed")),
            PatchLevelDays = ParseInt(Get(map, "patch_level_days")),
            BackupEnabled = ParseBool(Get(map, "backup_enabled")),
            OwnerEmail = NullIfEmpty(Get(map, "owner_email")),
            Vendor = NullIfEmpty(Get(map, "vendor")),
            Status = NullIfEmpty(Get(map, "status")),
            SourceSection = section
        };
    }

    private static string ResolveNodeKey(IReadOnlyDictionary<string, string> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Get(map, key);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return string.Empty;
    }

    private static string NormalizeAssetType(string deviceType, string section)
    {
        var lower = deviceType.ToLowerInvariant();
        if (section == "network_devices" || lower.Contains("firewall") || lower.Contains("router") ||
            lower.Contains("switch") || lower.Contains("vpn") || lower.Contains("wireless"))
            return "Network";
        if (section == "servers" || lower.Contains("server") || lower is "database" or "erp")
            return "Server";
        if (lower.Contains("cloud") || lower.Contains("saas") || lower.Contains("app service"))
            return "CloudApp";
        if (lower.Contains("identity") || lower.Contains("entra") || lower.Contains("ad"))
            return "Identity";
        if (lower is "desktop" or "laptop" or "workstation")
            return "Workstation";
        return deviceType;
    }

    private static string MapLinkKind(string? linkType) => linkType?.ToLowerInvariant() switch
    {
        "vpn tunnel" or "vpn" => "network",
        "fiber" or "ethernet" => "network",
        "deploy" or "deployment" => "deploy",
        "data" or "database" => "data",
        _ => "network"
    };

    private static SimulationParameters DeriveSimulationParameters(
        IReadOnlyList<TwinAssetDto> assets,
        IReadOnlyList<SecurityControlDto> controls)
    {
        var edrCoverage = assets.Count(a => a.AssetType is "Workstation" or "Server") == 0
            ? 0.6m
            : (decimal)assets.Count(a => a.EdrInstalled == true) /
              assets.Count(a => a.AssetType is "Workstation" or "Server");

        var edrControl = controls.FirstOrDefault(c => c.ControlType.Contains("EDR", StringComparison.OrdinalIgnoreCase));
        if (edrControl is not null)
            edrCoverage = Math.Max(edrCoverage, edrControl.CoveragePercent / 100m);

        var patchDays = assets.Where(a => a.PatchLevelDays.HasValue).Select(a => a.PatchLevelDays!.Value).DefaultIfEmpty(30).Average();

        return new SimulationParameters
        {
            EdrEffectiveness = Math.Round(Math.Clamp(edrCoverage, 0.2m, 0.98m), 2),
            PatchLagDays = (int)Math.Round(patchDays),
            BackupRpoHours = assets.Any(a => a.BackupEnabled == false && a.CriticalityTag?.Contains("Critical", StringComparison.OrdinalIgnoreCase) == true)
                ? 48 : 24
        };
    }

    private static string? InferOrgFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return null;
        return email.Split('@')[1];
    }

    private static IReadOnlyDictionary<string, string> RowDictionary(
        IReadOnlyList<string> headers,
        IReadOnlyList<string> row,
        Dictionary<string, string> columnMap)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (canonical, sourceHeader) in columnMap)
        {
            var idx = IndexOfHeader(headers, sourceHeader);
            if (idx >= 0 && idx < row.Count)
                dict[canonical] = row[idx].Trim();
        }
        return dict;
    }

    private static int IndexOfHeader(IReadOnlyList<string> headers, string header) =>
        headers.Select((h, i) => (h, i)).FirstOrDefault(x => x.h.Equals(header, StringComparison.OrdinalIgnoreCase)).i;

    private static string Get(IReadOnlyDictionary<string, string> map, params string[] keys)
    {
        foreach (var key in keys)
            if (map.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        return string.Empty;
    }

    private static string? NullIfEmpty(string v) => string.IsNullOrWhiteSpace(v) ? null : v;
    private static bool? ParseBool(string v) => v.ToLowerInvariant() switch { "yes" or "true" or "1" => true, "no" or "false" or "0" => false, _ => null };
    private static int? ParseInt(string v) => int.TryParse(v, out var i) ? i : null;
    private static decimal? ParseDecimal(string v) => decimal.TryParse(v, out var d) ? d : null;
}
