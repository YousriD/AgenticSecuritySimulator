using AgenticSecuritySimulator.Core.Models;

namespace AgenticSecuritySimulator.Core.Ingestion;

public static class LansweeperCsvParser
{
    public static IReadOnlyList<TwinAssetDto> Parse(string csvContent)
    {
        var rows = CsvReader.ReadAllRows(csvContent);
        if (rows.Count == 0)
            throw new InvalidOperationException("CSV is empty.");
        var columnMap = CsvColumnMapper.MapHeaders(rows[0]);
        return ParseRows(rows, columnMap);
    }

    internal static List<TwinAssetDto> ParseRows(
        IReadOnlyList<IReadOnlyList<string>> rows,
        Dictionary<string, string> columnMap)
    {
        var assets = new List<TwinAssetDto>();
        for (var i = 1; i < rows.Count; i++)
        {
            var headers = rows[0];
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (canonical, sourceHeader) in columnMap)
            {
                var idx = -1;
                for (var h = 0; h < headers.Count; h++)
                {
                    if (headers[h].Equals(sourceHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = h;
                        break;
                    }
                }
                if (idx >= 0 && idx < rows[i].Count)
                    dict[canonical] = rows[i][idx].Trim();
            }

            assets.Add(new TwinAssetDto
            {
                AssetName = Get(dict, "asset_name"),
                AssetType = Get(dict, "asset_type"),
                IpAddress = Null(dict, "ip_address"),
                Fqdn = Null(dict, "fqdn"),
                Ou = Null(dict, "ou"),
                Site = Null(dict, "site"),
                Os = Null(dict, "os"),
                Department = Null(dict, "department"),
                CriticalityTag = Null(dict, "criticality"),
                SourceSection = "lansweeper"
            });
        }
        return assets;
    }

    private static string Get(IReadOnlyDictionary<string, string> map, string key) =>
        map.TryGetValue(key, out var v) ? v : string.Empty;

    private static string? Null(IReadOnlyDictionary<string, string> map, string key)
    {
        var v = Get(map, key);
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }
}
