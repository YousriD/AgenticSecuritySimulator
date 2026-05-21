namespace AgenticSecuritySimulator.Core.Ingestion;

internal static class CsvColumnMapper
{
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dataset_type"] = ["dataset_type", "type", "section", "record_type"],
        ["asset_name"] = ["assetname", "asset_name", "hostname", "name", "device_name", "host_name"],
        ["asset_type"] = ["assettype", "asset_type", "type", "category"],
        ["device_type"] = ["device_type", "device type", "devicetype"],
        ["ip_address"] = ["ipaddress", "ip_address", "ip", "ipv4"],
        ["fqdn"] = ["fqdn", "dns_name", "fully_qualified_domain_name"],
        ["ou"] = ["ou", "organizational_unit", "ad_ou"],
        ["site"] = ["site", "location", "office", "branch"],
        ["os"] = ["os", "operating_system", "operating system"],
        ["department"] = ["department", "dept", "business_unit", "division"],
        ["criticality"] = ["criticality", "criticalitytag", "criticality_tag", "risk", "priority"],
        ["role"] = ["role", "server_role", "function"],
        ["environment"] = ["environment", "env", "tier"],
        ["vlan"] = ["vlan", "vlan_id", "subnet"],
        ["edr_installed"] = ["edr_installed", "edr", "endpoint_protection"],
        ["patch_level_days"] = ["patch_level_days", "patch_days", "days_since_patch", "lastpatched"],
        ["backup_enabled"] = ["backup_enabled", "backup", "has_backup"],
        ["owner_email"] = ["owner_email", "owner", "contact"],
        ["vendor"] = ["vendor", "manufacturer"],
        ["status"] = ["status", "state", "operational_status"],
        ["source_node"] = ["source_node", "source", "from", "from_node"],
        ["destination_node"] = ["destination_node", "destination", "to", "to_node"],
        ["link_type"] = ["link_type", "link_kind", "connection_type"],
        ["encrypted"] = ["encrypted", "is_encrypted", "tls"],
        ["control_id"] = ["control_id", "controlid"],
        ["control_type"] = ["control_type", "controltype"],
        ["platform"] = ["platform", "product", "solution"],
        ["coverage_percent"] = ["coverage_percent", "coverage", "coverage_pct"],
        ["owner_team"] = ["owner_team", "team"],
        ["compliance_mapping"] = ["compliance_mapping", "compliance", "framework"],
        ["primary_id"] = ["primary_id", "asset_id", "id", "server_id", "network_id"]
    };

    public static Dictionary<string, string> MapHeaders(IReadOnlyList<string> headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            var normalized = Normalize(header);
            foreach (var (canonical, aliases) in Aliases)
            {
                if (aliases.Any(a => Normalize(a) == normalized))
                {
                    result[canonical] = header;
                    break;
                }
            }
        }

        return result;
    }

    public static List<Models.ColumnMapping> DescribeMappings(Dictionary<string, string> map) =>
        map.Select(kv => new Models.ColumnMapping { CanonicalField = kv.Key, SourceColumn = kv.Value }).ToList();

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
}
