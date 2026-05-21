using System.Text.Json;
using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Models;

namespace AgenticSecuritySimulator.Core.Topology;

public static class TwinGraphBuilder
{
    public static (Twin Twin, Organization Organization) BuildFromAssets(
        string organizationName,
        string twinName,
        string source,
        IReadOnlyList<TwinAssetDto> assets,
        IReadOnlyList<TwinDependencyDto>? explicitDependencies = null,
        IReadOnlyList<SecurityControlDto>? controls = null)
    {
        var org = new Organization { Name = organizationName };
        var twin = new Twin
        {
            OrganizationId = org.Id,
            Organization = org,
            Name = twinName,
            Source = source
        };

        var nodeByKey = new Dictionary<string, TwinNode>(StringComparer.OrdinalIgnoreCase);
        var aliasIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in assets)
        {
            var node = new TwinNode
            {
                TwinId = twin.Id,
                Twin = twin,
                ExternalKey = asset.AssetName,
                DisplayName = asset.AssetName,
                NodeType = asset.AssetType,
                Zone = BuildZone(asset),
                CriticalityWeight = MapCriticality(asset.CriticalityTag, asset.AssetType),
                PropertiesJson = JsonSerializer.Serialize(asset)
            };
            twin.Nodes.Add(node);
            nodeByKey[asset.AssetName] = node;
            RegisterAlias(aliasIndex, asset.AssetName, asset.AssetName);
            if (!string.IsNullOrEmpty(asset.IpAddress))
                RegisterAlias(aliasIndex, asset.IpAddress, asset.AssetName);
        }

        foreach (var control in controls ?? [])
        {
            var controlNode = new TwinNode
            {
                TwinId = twin.Id,
                Twin = twin,
                ExternalKey = control.ControlId,
                DisplayName = $"{control.ControlType} ({control.Platform})",
                NodeType = "Control",
                Zone = control.OwnerTeam,
                CriticalityWeight = Math.Clamp(control.CoveragePercent / 100m, 0.3m, 1m),
                PropertiesJson = JsonSerializer.Serialize(control)
            };
            twin.Nodes.Add(controlNode);
            nodeByKey[control.ControlId] = controlNode;
            RegisterAlias(aliasIndex, control.ControlId, control.ControlId);
        }

        var hasExplicitLinks = false;
        foreach (var dep in explicitDependencies ?? [])
        {
            if (TryResolveNode(dep.From, nodeByKey, aliasIndex, out var from) &&
                TryResolveNode(dep.To, nodeByKey, aliasIndex, out var to))
            {
                AddEdge(twin, from, to, dep.Kind, isSynthetic: false);
                hasExplicitLinks = true;
            }
        }

        if (!hasExplicitLinks)
            ApplyDefaultSyntheticEdges(twin, nodeByKey);

        return (twin, org);
    }

    private static string BuildZone(TwinAssetDto asset)
    {
        var parts = new[] { asset.Site, asset.Department, asset.Environment }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var zone = string.Join(" / ", parts);
        return string.IsNullOrEmpty(zone) ? "Default" : zone;
    }

    private static void RegisterAlias(Dictionary<string, string> index, string alias, string canonical)
    {
        if (!string.IsNullOrWhiteSpace(alias) && !index.ContainsKey(alias))
            index[alias] = canonical;
    }

    private static bool TryResolveNode(
        string key,
        IReadOnlyDictionary<string, TwinNode> nodes,
        IReadOnlyDictionary<string, string> aliases,
        out TwinNode node)
    {
        if (nodes.TryGetValue(key, out node!))
            return true;

        if (aliases.TryGetValue(key, out var canonical) && nodes.TryGetValue(canonical, out node!))
            return true;

        // Fuzzy: SRV-WEBAPP-05 vs SRV-WEBAPP-5, prefix match on hostname
        var match = nodes.Values.FirstOrDefault(n =>
            n.DisplayName.Equals(key, StringComparison.OrdinalIgnoreCase) ||
            n.ExternalKey.Equals(key, StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith(n.DisplayName, StringComparison.OrdinalIgnoreCase));

        node = match!;
        return match is not null;
    }

    private static void ApplyDefaultSyntheticEdges(Twin twin, IReadOnlyDictionary<string, TwinNode> nodes)
    {
        void Link(string from, string to, string kind)
        {
            if (nodes.TryGetValue(from, out var f) && nodes.TryGetValue(to, out var t))
                AddEdge(twin, f, t, kind, isSynthetic: true);
        }

        Link("WS-FIN-042", "SRV-FILE01", "network");
        Link("WS-FIN-042", "SRV-PAYROLL", "network");
        Link("SRV-CICD", "AZ-APP-PAYAPI", "deploy");
        Link("DC01", "ENTRA-ID", "identity");
    }

    private static void AddEdge(Twin twin, TwinNode from, TwinNode to, string kind, bool isSynthetic)
    {
        if (twin.Edges.Any(e => e.FromNodeId == from.Id && e.ToNodeId == to.Id && e.Kind == kind))
            return;

        twin.Edges.Add(new TwinEdge
        {
            TwinId = twin.Id,
            Twin = twin,
            FromNodeId = from.Id,
            FromNode = from,
            ToNodeId = to.Id,
            ToNode = to,
            Kind = kind,
            IsSynthetic = isSynthetic
        });
    }

    private static decimal MapCriticality(string? tag, string assetType)
    {
        var score = tag?.ToLowerInvariant() switch
        {
            "crown-jewel" or "critical" => 1.0m,
            "high" => 0.75m,
            "medium" => 0.5m,
            "low" => 0.3m,
            _ => 0.4m
        };

        if (assetType is "Server" or "Identity" && score < 0.7m)
            score = 0.7m;
        if (assetType == "Network" && score < 0.6m)
            score = 0.6m;
        return score;
    }
}
