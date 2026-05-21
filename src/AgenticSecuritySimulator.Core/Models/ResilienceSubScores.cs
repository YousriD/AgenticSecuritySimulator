namespace AgenticSecuritySimulator.Core.Models;

public sealed class ResilienceSubScores
{
    public decimal Availability { get; init; }
    public decimal Detection { get; init; }
    public decimal Containment { get; init; }
    public decimal Recovery { get; init; }
    public decimal BlastRadius { get; init; }
    public decimal Composite { get; init; }

    public string WeakestDimension =>
        new (string Name, decimal Value)[]
        {
            ("Availability", Availability),
            ("Detection", Detection),
            ("Containment", Containment),
            ("Recovery", Recovery),
            ("BlastRadius", BlastRadius)
        }.MinBy(x => x.Value).Name;
}
