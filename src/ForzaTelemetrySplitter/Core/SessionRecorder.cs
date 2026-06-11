using System.Diagnostics;
using System.Text;

namespace ForzaTelemetrySplitter.Core;

/// <summary>
/// Records a live telemetry session to a `.fts` capture file for bug reports and offline analysis.
///
/// File format (little-endian):
///   header: 4-byte magic "FTS1", then int32 version (1)
///   per packet: int64 delta-ticks (Stopwatch ticks since the previous packet; 0 for the first),
///               uint16 length, then <length> raw bytes
///
/// Captures are byte-exact and game-agnostic. Writing is append-only and happens off the forwarding
/// hot path (the engine calls Write AFTER forwarding), so recording never delays live tools.
/// </summary>
public sealed class SessionRecorder : IDisposable
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("FTS1");
    private const int Version = 1;

    private readonly BinaryWriter _writer;
    private readonly object _gate = new();
    private long _lastTicks;
    private bool _disposed;

    public string Path { get; }
    public long PacketsWritten { get; private set; }

    public SessionRecorder(string path)
    {
        Path = path;
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        _writer = new BinaryWriter(stream);
        _writer.Write(Magic);
        _writer.Write(Version);
        _lastTicks = Stopwatch.GetTimestamp();
    }

    /// <summary>Append one datagram. Safe to call from the receive thread; best-effort (a write
    /// failure is swallowed so recording problems never crash the relay).</summary>
    public void Write(byte[] buffer, int length)
    {
        if (length is < 0 or > ushort.MaxValue) return;
        lock (_gate)
        {
            if (_disposed) return;
            try
            {
                long now = Stopwatch.GetTimestamp();
                _writer.Write(now - _lastTicks);
                _writer.Write((ushort)length);
                _writer.Write(buffer, 0, length);
                _lastTicks = now;
                PacketsWritten++;
            }
            catch
            {
                // Disk full / IO error — stop recording silently rather than disrupt forwarding.
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _writer.Flush(); _writer.Dispose(); } catch { }
        }
    }
}
