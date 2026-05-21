using AgenticSecuritySimulator.Core.Models;
using AgenticSecuritySimulator.Web.Services;

namespace AgenticSecuritySimulator.Web.Api;

public static class TwinEndpoints
{
    public static void MapTwinEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/twins");

        group.MapPost("/", async (TwinImportRequest request, TwinImportService importer, CancellationToken ct) =>
        {
            var id = await importer.ImportRequestAsync(request, ct);
            return Results.Created($"/api/v1/twins/{id}", new { twinId = id });
        });

        group.MapPost("/import-sample", async (TwinImportService importer, CancellationToken ct) =>
        {
            var id = await importer.ImportSampleAsync(ct);
            return Results.Ok(new { twinId = id });
        });
    }
}
