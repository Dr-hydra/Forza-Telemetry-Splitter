namespace ForzaTelemetrySplitter.Core;

/// <summary>
/// A fixed-size, in-memory rolling history of packets-per-second samples (one per wall-clock second,
/// last hour). A ring buffer: appending overwrites the oldest sample, so it self-trims and never
/// grows. Nothing is ever written to disk.
///
/// A <see cref="float.NaN"/> sample marks a second with no data (the engine was stopped), so the
/// chart can break the line at gaps instead of drawing a misleading zero.
///
/// Thread-safe: the engine writes (Add) on its side; the UI reads (Snapshot) on the UI thread. Both
/// take a short lock; the locked region is just an array touch (microseconds).
/// </summary>
public sealed class ActivityHistory
{
    public const int OneHourSeconds = 3600;

    private readonly object _gate = new();
    private readonly float[] _buf;
    private int _head;   // next write index
    private int _count;  // number of valid samples (< capacity until full)

    public int Capacity => _buf.Length;

    public ActivityHistory(int capacity = OneHourSeconds)
    {
        _buf = new float[Math.Max(1, capacity)];
    }

    /// <summary>Append one sample (use float.NaN for "no data this second").</summary>
    public void Add(float value)
    {
        lock (_gate)
        {
            _buf[_head] = value;
            _head = (_head + 1) % _buf.Length;
            if (_count < _buf.Length) _count++;
        }
    }

    /// <summary>
    /// Copy the samples in logical order (oldest → newest) into <paramref name="dst"/> (which must be
    /// at least <see cref="Capacity"/> long). Returns the number written.
    /// </summary>
    public int Snapshot(float[] dst)
    {
        lock (_gate)
        {
            int start = (_head - _count + _buf.Length) % _buf.Length;
            for (int i = 0; i < _count; i++)
                dst[i] = _buf[(start + i) % _buf.Length];
            return _count;
        }
    }

    /// <summary>Clear all history.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _head = 0;
            _count = 0;
        }
    }
}
