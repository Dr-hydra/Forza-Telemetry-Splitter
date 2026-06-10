using System.Net;
using System.Text.Json.Serialization;

namespace ForzaTelemetrySplitter.Core;

/// <summary>
/// A single forwarding target: a friendly name plus an IP:port that a downstream tool
/// (VirtualTCU, a tuner, a dashboard) listens on. The splitter resends each Forza packet
/// to every enabled destination.
/// </summary>
public sealed class Destination
{
    public string Name { get; set; } = "Destination";
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5556;
    public bool Enabled { get; set; } = true;

    /// <summary>Running count of packets forwarded to this destination (not persisted).</summary>
    [JsonIgnore]
    public long ForwardedCount;

    /// <summary>
    /// Cached endpoint, rebuilt whenever Ip/Port change. Avoids re-parsing on the hot path.
    /// </summary>
    [JsonIgnore]
    private IPEndPoint? _endPoint;

    [JsonIgnore]
    private string? _cachedIp;

    [JsonIgnore]
    private int _cachedPort = -1;

    /// <summary>
    /// Returns the cached <see cref="IPEndPoint"/>, rebuilding it only if Ip/Port changed.
    /// Returns null if the IP cannot be parsed (surfaced as a validation error in the UI).
    /// </summary>
    public IPEndPoint? GetEndPoint()
    {
        if (_endPoint is not null && _cachedIp == Ip && _cachedPort == Port)
            return _endPoint;

        if (!IPAddress.TryParse(Ip, out var addr))
            return null;

        _endPoint = new IPEndPoint(addr, Port);
        _cachedIp = Ip;
        _cachedPort = Port;
        return _endPoint;
    }

    public override string ToString() => $"{Name} ({Ip}:{Port})";
}
