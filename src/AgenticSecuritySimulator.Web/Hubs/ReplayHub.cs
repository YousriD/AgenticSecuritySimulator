using AgenticSecuritySimulator.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace AgenticSecuritySimulator.Web.Hubs;

/// <summary>
/// SignalR hub that streams simulation replay events to connected clients.
/// Clients join a session group keyed by batchId so multiple browser tabs
/// can watch the same replay in sync.
/// </summary>
public sealed class ReplayHub : Hub
{
    public async Task JoinSession(string batchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, batchId);
    }

    public async Task LeaveSession(string batchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, batchId);
    }

    /// <summary>Called by the server-side play loop to push a step to all watchers.</summary>
    public static Task BroadcastStep(IHubContext<ReplayHub> hub, string batchId, ReplayStepDto step) =>
        hub.Clients.Group(batchId).SendAsync("OnStep", step);

    /// <summary>Called when playback finishes or is reset.</summary>
    public static Task BroadcastStatus(IHubContext<ReplayHub> hub, string batchId, string status) =>
        hub.Clients.Group(batchId).SendAsync("OnStatus", status);
}

public sealed record ReplayStepDto(
    int StepIndex,
    int TotalSteps,
    string Actor,
    string? TechniqueId,
    string? NodeId,
    string Outcome,
    string Message,
    int TimestampOffsetMs);
