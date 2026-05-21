using AgenticSecuritySimulator.Core.Ingestion;
using AgenticSecuritySimulator.Core.Models;
using AgenticSecuritySimulator.Core.Topology;
using AgenticSecuritySimulator.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticSecuritySimulator.Web.Services;

public sealed class TwinImportService(AppDbContext db, IWebHostEnvironment env)
{
    public InfrastructureImportResult AnalyzeCsv(string csvContent) =>
        InfrastructureCsvParser.Analyze(csvContent);

    public async Task<(Guid TwinId, InfrastructureImportResult Result)> ImportCsvAsync(
        string organizationName,
        string twinName,
        string csvContent,
        CancellationToken ct = default)
    {
        var result = InfrastructureCsvParser.Parse(csvContent);
        var orgName = string.IsNullOrWhiteSpace(organizationName)
            ? DomainFromHint(result.Report.OrganizationHint) ?? "Imported Organization"
            : organizationName;

        var twinId = await SaveTwinAsync(orgName, twinName, result.Report.DetectedFormat.ToString(), result, ct);
        return (twinId, result);
    }

    public async Task<Guid> ImportRequestAsync(TwinImportRequest request, CancellationToken ct = default)
    {
        var result = new InfrastructureImportResult
        {
            Assets = request.Assets,
            Dependencies = request.Dependencies,
            Controls = request.Controls,
            SuggestedParameters = request.SimulationParameters ?? new SimulationParameters(),
            Report = new CsvParseReport { DetectedFormat = CsvFormat.Generic, RowCountsBySection = new Dictionary<string, int> { ["assets"] = request.Assets.Count } }
        };
        return await SaveTwinAsync(request.OrganizationName, request.TwinName, "api", result, ct);
    }

    public async Task<Guid> ImportSampleAsync(CancellationToken ct = default)
    {
        var path = ContentPaths.SampleCsvPath(env);
        var csv = await File.ReadAllTextAsync(path, ct);
        var (twinId, _) = await ImportCsvAsync("Contoso", "POC Twin", csv, ct);
        return twinId;
    }

    public async Task<Guid> ImportCompanyTwinAsync(CancellationToken ct = default)
    {
        var path = ContentPaths.CompanyTwinCsvPath(env);
        var csv = await File.ReadAllTextAsync(path, ct);
        var analyzed = InfrastructureCsvParser.Analyze(csv);
        var (twinId, _) = await ImportCsvAsync(
            DomainFromHint(analyzed.Report.OrganizationHint) ?? "Corp Example",
            "Company Digital Twin",
            csv,
            ct);
        return twinId;
    }

    private async Task<Guid> SaveTwinAsync(
        string organizationName,
        string twinName,
        string source,
        InfrastructureImportResult result,
        CancellationToken ct)
    {
        var (twin, org) = TwinGraphBuilder.BuildFromAssets(
            organizationName,
            twinName,
            source,
            result.Assets,
            result.Dependencies,
            result.Controls);

        var existingOrg = await db.Organizations.FirstOrDefaultAsync(o => o.Name == organizationName, ct);
        if (existingOrg is null)
            db.Organizations.Add(org);
        else
        {
            org = existingOrg;
            twin.OrganizationId = org.Id;
            twin.Organization = org;
        }

        db.Twins.Add(twin);
        await db.SaveChangesAsync(ct);
        return twin.Id;
    }

    private static string? DomainFromHint(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return null;
        return hint.Contains('.') ? hint.Split('.')[0] : hint;
    }
}
