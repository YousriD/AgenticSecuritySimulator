using AgenticSecuritySimulator.Core.Models;

namespace AgenticSecuritySimulator.Core.Ingestion;

public static class TopologyInference
{
    public static List<TwinDependencyDto> InferAdditionalEdges(
        IReadOnlyList<TwinAssetDto> assets,
        IReadOnlyList<TwinDependencyDto> explicitLinks,
        IReadOnlyList<SecurityControlDto> controls)
    {
        var result = new List<TwinDependencyDto>();
        var byKey = assets.ToDictionary(a => a.AssetName, a => a, StringComparer.OrdinalIgnoreCase);
        var existing = explicitLinks
            .Select(l => EdgeKey(l.From, l.To, l.Kind))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        void Add(string from, string to, string kind, bool synthetic = true)
        {
            if (!byKey.ContainsKey(from) || !byKey.ContainsKey(to))
                return;
            var key = EdgeKey(from, to, kind);
            if (!existing.Add(key))
                return;
            result.Add(new TwinDependencyDto { From = from, To = to, Kind = kind });
        }

        // Resolve link endpoints: SRV-WEBAPP-05 may match hostname on a server asset
        foreach (var link in explicitLinks)
        {
            ResolveEndpoint(byKey, link.From);
            ResolveEndpoint(byKey, link.To);
        }

        // Same VLAN -> same broadcast domain
        foreach (var group in assets.Where(a => !string.IsNullOrEmpty(a.Vlan)).GroupBy(a => a.Vlan))
        {
            var members = group.Take(12).ToList();
            for (var i = 1; i < members.Count; i++)
                Add(members[0].AssetName, members[i].AssetName, "network");
        }

        // Same site: workstations to local network gear / servers
        var networks = assets.Where(a => a.AssetType == "Network").ToList();
        var servers = assets.Where(a => a.AssetType == "Server").ToList();
        var workstations = assets.Where(a => a.AssetType == "Workstation").ToList();

        foreach (var ws in workstations.Take(80))
        {
            var siteNet = networks.FirstOrDefault(n => n.Site == ws.Site);
            if (siteNet is not null)
                Add(ws.AssetName, siteNet.AssetName, "network");

            var deptServer = servers.FirstOrDefault(s =>
                s.Department == ws.Department || s.Role?.Contains(ws.Department ?? "", StringComparison.OrdinalIgnoreCase) == true);
            if (deptServer is not null)
                Add(ws.AssetName, deptServer.AssetName, "data");
        }

        // Crown-jewel / critical servers: AD, ERP, Database, SIEM hubs
        var ad = servers.FirstOrDefault(s => s.Role?.Contains("Active Directory", StringComparison.OrdinalIgnoreCase) == true);
        var erp = servers.FirstOrDefault(s => s.Role?.Contains("ERP", StringComparison.OrdinalIgnoreCase) == true);
        var db = servers.FirstOrDefault(s => s.Role?.Contains("Database", StringComparison.OrdinalIgnoreCase) == true);
        var siem = servers.FirstOrDefault(s => s.Role?.Contains("SIEM", StringComparison.OrdinalIgnoreCase) == true);

        if (ad is not null)
        {
            foreach (var ws in workstations.Take(40))
                Add(ws.AssetName, ad.AssetName, "identity");
        }

        if (erp is not null && db is not null)
            Add(erp.AssetName, db.AssetName, "data");

        if (siem is not null)
        {
            foreach (var srv in servers.Where(s => s.Role?.Contains("SIEM", StringComparison.OrdinalIgnoreCase) != true).Take(10))
                Add(srv.AssetName, siem.AssetName, "monitoring");
        }

        // Perimeter: firewalls to core routers at same site
        foreach (var fw in networks.Where(n => n.AssetType == "Network" && (n.Role?.Contains("Firewall") == true || n.AssetName.Contains("FIREWALL", StringComparison.OrdinalIgnoreCase))))
        {
            var core = networks.FirstOrDefault(n =>
                n.Site == fw.Site && n.AssetName.Contains("CORE", StringComparison.OrdinalIgnoreCase));
            if (core is not null)
                Add(fw.AssetName, core.AssetName, "network");
        }

        // Security controls as governance edges to representative assets
        var edr = controls.FirstOrDefault(c => c.ControlType.Contains("EDR", StringComparison.OrdinalIgnoreCase));
        if (edr is not null)
        {
            foreach (var target in assets.Where(a => a.EdrInstalled == true).Take(25))
                Add(edr.ControlId, target.AssetName, "protects");
        }

        return result;
    }

    private static string EdgeKey(string from, string to, string kind) => $"{from}|{to}|{kind}";

    private static void ResolveEndpoint(Dictionary<string, TwinAssetDto> byKey, string name)
    {
        if (byKey.ContainsKey(name))
            return;

        // Links use hostname (SRV-WEBAPP-05) while server rows may use primary_id — match by hostname field
        var match = byKey.Values.FirstOrDefault(a =>
            a.AssetName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            (a.Fqdn?.Contains(name, StringComparison.OrdinalIgnoreCase) == true));
        // No mutation — caller must ensure asset keys align; alias map done in graph builder
    }
}
