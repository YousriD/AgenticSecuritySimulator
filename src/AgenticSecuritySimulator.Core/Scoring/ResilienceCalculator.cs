using AgenticSecuritySimulator.Core.Models;

namespace AgenticSecuritySimulator.Core.Scoring;

public static class ResilienceCalculator
{
    public static ResilienceSubScores Calculate(
        SimulationParameters parameters,
        int compromisedHighValueNodes,
        int totalHighValueNodes,
        decimal simulatedMttdMinutes,
        decimal simulatedMttcMinutes,
        bool recoverySucceeded)
    {
        var availability = totalHighValueNodes == 0
            ? 1m
            : Math.Clamp(1m - (decimal)compromisedHighValueNodes / totalHighValueNodes, 0m, 1m);

        var detection = simulatedMttdMinutes <= parameters.MttdTargetMinutes
            ? 1m
            : Math.Clamp(parameters.MttdTargetMinutes / simulatedMttdMinutes, 0m, 1m);

        var containment = simulatedMttcMinutes <= parameters.MttcTargetMinutes
            ? 1m
            : Math.Clamp(parameters.MttcTargetMinutes / simulatedMttcMinutes, 0m, 1m);

        var recovery = recoverySucceeded
            ? Math.Clamp(1m - (parameters.BackupRpoHours / 72m), 0.2m, 1m)
            : 0.2m;

        var blastRadius = totalHighValueNodes == 0
            ? 0m
            : Math.Clamp((decimal)compromisedHighValueNodes / totalHighValueNodes, 0m, 1m);

        var composite = 0.25m * availability
            + 0.2m * detection
            + 0.2m * containment
            + 0.2m * recovery
            + 0.15m * (1m - blastRadius);

        return new ResilienceSubScores
        {
            Availability = Math.Round(availability, 4),
            Detection = Math.Round(detection, 4),
            Containment = Math.Round(containment, 4),
            Recovery = Math.Round(recovery, 4),
            BlastRadius = Math.Round(blastRadius, 4),
            Composite = Math.Round(composite * 100m, 2)
        };
    }
}
