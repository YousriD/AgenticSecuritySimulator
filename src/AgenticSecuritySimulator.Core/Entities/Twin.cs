namespace AgenticSecuritySimulator.Core.Entities;

public class Twin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "csv";
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<TwinNode> Nodes { get; set; } = [];
    public ICollection<TwinEdge> Edges { get; set; } = [];
}
