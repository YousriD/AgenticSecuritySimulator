using AgenticSecuritySimulator.Core.Entities;

namespace AgenticSecuritySimulator.Web.Services;

public sealed class TopologyLayoutService
{
    public TopologyLayout Compute(Twin twin, TopologyViewOptions options)
    {
        var nodes = twin.Nodes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var q = options.Search.Trim();
            nodes = nodes.Where(n =>
                n.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.NodeType.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (n.Zone?.Contains(q, StringComparison.OrdinalIgnoreCase) == true));
        }

        nodes = options.ViewMode switch
        {
            TopologyViewMode.CriticalOnly => nodes.Where(n => n.CriticalityWeight >= 0.7m || n.NodeType is "Network" or "Control"),
            TopologyViewMode.Infrastructure => nodes.Where(n =>
                (n.NodeType != "Workstation" && n.NodeType != "Control") || n.CriticalityWeight >= 0.75m),
            TopologyViewMode.ByZone when !string.IsNullOrEmpty(options.SelectedZone) => nodes.Where(n => n.Zone == options.SelectedZone),
            _ => nodes
        };

        if (!string.IsNullOrEmpty(options.NodeTypeFilter))
            nodes = nodes.Where(n => n.NodeType == options.NodeTypeFilter);

        var nodeList = nodes.ToList();
        var nodeIds = nodeList.Select(n => n.Id).ToHashSet();
        var edges = twin.Edges
            .Where(e => nodeIds.Contains(e.FromNodeId) && nodeIds.Contains(e.ToNodeId))
            .Take(options.MaxEdges)
            .ToList();

        var zones = nodeList.GroupBy(n => n.Zone ?? "Unknown").OrderBy(g => g.Key).ToList();
        var positions = new Dictionary<Guid, (double X, double Y)>();
        var zoneBounds = new List<ZoneBounds>();

        const double zoneWidth = 200;
        const double zoneHeight = 140;
        var col = 0;
        var row = 0;

        foreach (var zoneGroup in zones)
        {
            var originX = 40 + col * (zoneWidth + 30);
            var originY = 40 + row * (zoneHeight + 30);
            var members = zoneGroup.ToList();

            for (var i = 0; i < members.Count; i++)
            {
                var cx = originX + 50 + (i % 4) * 36;
                var cy = originY + 40 + (i / 4) * 36;
                positions[members[i].Id] = (cx, cy);
            }

            zoneBounds.Add(new ZoneBounds(zoneGroup.Key, originX, originY, zoneWidth, zoneHeight, members.Count));
            col++;
            if (col >= 4) { col = 0; row++; }
        }

        var width = Math.Max(900, (col + 1) * (zoneWidth + 30) + 80);
        var height = Math.Max(500, (row + 1) * (zoneHeight + 30) + 100);

        return new TopologyLayout(nodeList, edges, positions, zoneBounds, width, height);
    }
}

public sealed class TopologyViewOptions
{
    public TopologyViewMode ViewMode { get; set; } = TopologyViewMode.Infrastructure;
    public string? SelectedZone { get; set; }
    public string? NodeTypeFilter { get; set; }
    public string? Search { get; set; }
    public int MaxEdges { get; set; } = 200;
}

public enum TopologyViewMode
{
    All,
    Infrastructure,
    CriticalOnly,
    ByZone
}

public sealed record ZoneBounds(string Zone, double X, double Y, double Width, double Height, int NodeCount);

public sealed record TopologyLayout(
    IReadOnlyList<TwinNode> Nodes,
    IReadOnlyList<TwinEdge> Edges,
    IReadOnlyDictionary<Guid, (double X, double Y)> Positions,
    IReadOnlyList<ZoneBounds> Zones,
    double Width,
    double Height);
