using System.Collections.Concurrent;

namespace AgenticSecuritySimulator.Web.Services;

/// <summary>
/// Singleton that tracks active replay sessions so pause/resume state
/// survives Blazor component re-renders and is shared across SignalR connections.
/// </summary>
public sealed class ReplayStateService
{
    private readonly ConcurrentDictionary<Guid, ReplaySession> _sessions = new();

    public ReplaySession GetOrCreate(Guid batchId) =>
        _sessions.GetOrAdd(batchId, id => new ReplaySession(id));

    public void Remove(Guid batchId) =>
        _sessions.TryRemove(batchId, out _);
}

public sealed class ReplaySession(Guid batchId)
{
    public Guid BatchId { get; } = batchId;
    public int CurrentStep { get; set; } = -1;
    public bool IsPlaying { get; set; }
    public int SpeedMs { get; set; } = 1200;
    public string RunMode { get; set; } = "median"; // median | worst | best
    public HashSet<int> Bookmarks { get; } = [];
    public CancellationTokenSource? PlayCts { get; set; }

    public void CancelPlay()
    {
        PlayCts?.Cancel();
        PlayCts?.Dispose();
        PlayCts = null;
        IsPlaying = false;
    }
}
