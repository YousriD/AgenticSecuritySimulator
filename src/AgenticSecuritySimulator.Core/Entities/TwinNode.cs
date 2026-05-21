namespace AgenticSecuritySimulator.Core.Entities;

public class TwinNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TwinId { get; set; }
    public Twin Twin { get; set; } = null!;
    public string ExternalKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public decimal CriticalityWeight { get; set; }
    public string? PropertiesJson { get; set; }
}
