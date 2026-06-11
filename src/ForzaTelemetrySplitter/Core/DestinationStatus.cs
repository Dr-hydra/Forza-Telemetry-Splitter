namespace ForzaTelemetrySplitter.Core;

/// <summary>
/// What the per-destination status dot shows. Labeled honestly from the sender's perspective: with
/// fire-and-forget UDP we cannot confirm the receiving tool got anything, so the "good" state is
/// "Forwarding" (we are sending), never "Connected".
/// </summary>
public enum DestinationStatus
{
    Disabled,    // grey  — destination turned off by the user
    Forwarding,  // green — enabled, engine receiving, packets flowing to this destination
    Idle,        // amber — enabled, but no packets flowing (Forza paused / in menus / not sending)
    Error,       // red   — last send to this destination failed (local socket error)
}

public static class DestinationStatusLogic
{
    /// <summary>
    /// Derive the status for a destination. Pure and testable (no UI). The caller supplies whether the
    /// engine is currently receiving Forza packets, and whether this destination's forwarded count has
    /// increased since the previous tick (i.e. it's actively being fed right now).
    /// </summary>
    public static DestinationStatus Derive(bool enabled, bool engineReceiving, bool countIncreased, bool lastSendFailed)
    {
        if (!enabled) return DestinationStatus.Disabled;
        if (lastSendFailed) return DestinationStatus.Error;
        if (engineReceiving && countIncreased) return DestinationStatus.Forwarding;
        return DestinationStatus.Idle;
    }
}
