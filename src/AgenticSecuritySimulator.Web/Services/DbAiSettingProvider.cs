using AgenticSecuritySimulator.Core.Entities;
using AgenticSecuritySimulator.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticSecuritySimulator.Web.Services;

public sealed class DbAiSettingProvider : IAiSettingProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DbAiSettingProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<AiProviderSetting?> GetActiveSettingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.AiProviderSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IsActive, cancellationToken);
        }
        catch (Exception)
        {
            // Table may not exist yet on SQL Server before migration runs — return null (POC/rule-based mode)
            return null;
        }
    }
}
