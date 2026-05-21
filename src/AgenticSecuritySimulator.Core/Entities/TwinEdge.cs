namespace AgenticSecuritySimulator.Core.Entities;

public class TwinEdge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TwinId { get; set; }
    public Twin Twin { get; set; } = null!;
    public Guid FromNodeId { get; set; }
    public TwinNode FromNode { get; set; } = null!;
    public Guid ToNodeId { get; set; }
    public TwinNode ToNode { get; set; } = null!;
    public string Kind { get; set; } = string.Empty;
    public bool IsSynthetic { get; set; }
}
