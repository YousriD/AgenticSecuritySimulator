using AgenticSecuritySimulator.Agents;
using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Core.Scenarios;
using AgenticSecuritySimulator.Simulation;
using AgenticSecuritySimulator.Web.Api;
using AgenticSecuritySimulator.Web.Components;
using AgenticSecuritySimulator.Web.Data;
using AgenticSecuritySimulator.Web.Hubs;
using AgenticSecuritySimulator.Web.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var dbProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
    ? builder.Configuration.GetConnectionString("SqlServer")
    : builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=agentic-security.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(connectionString);
    else
        options.UseSqlite(connectionString);
});
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<TwinImportService>();
builder.Services.AddScoped<BatchRunService>();
builder.Services.AddScoped<SimulationService>();
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(o => o.DetailedErrors = true);
builder.Services.AddSignalR(h => h.ClientTimeoutInterval = TimeSpan.FromMinutes(5));
builder.Services.AddSingleton<TopologyLayoutService>();
builder.Services.AddSingleton<SimulationBatchOrchestrator>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiSettingProvider, DbAiSettingProvider>();
builder.Services.AddSingleton<ReplayStateService>();
builder.Services.AddSimulationAgents(useAiPlanners: false);

var app = builder.Build();

await SeedDataAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapTwinEndpoints();
app.MapHub<ReplayHub>("/hubs/replay");

app.Run();

static async Task SeedDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (await db.AttackScenarios.AnyAsync())
        return;

    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var scenarios = ScenarioCatalog.LoadFromDirectory(ContentPaths.ScenariosDirectory(env));
    foreach (var scenario in scenarios)
    {
        db.AttackScenarios.Add(new AttackScenario
        {
            Id = scenario.Id,
            Name = scenario.Name,
            Description = scenario.Description,
            CriticalityWeight = scenario.CriticalityWeight,
            DefinitionJson = System.Text.Json.JsonSerializer.Serialize(scenario)
        });
    }

    await db.SaveChangesAsync();
}
