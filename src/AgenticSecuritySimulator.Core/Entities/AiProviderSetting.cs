namespace AgenticSecuritySimulator.Core.Entities;

public class AiProviderSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProviderType { get; set; } = string.Empty; // "Ollama", "LMStudio", "AzureOpenAI"
    public string DisplayName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; }
}

public interface IAiSettingProvider
{
    Task<AiProviderSetting?> GetActiveSettingAsync(CancellationToken cancellationToken = default);
}
