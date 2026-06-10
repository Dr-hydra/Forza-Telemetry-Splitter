using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ForzaTelemetrySplitter.Core;

/// <summary>
/// Immutable snapshot of engine state, handed to the UI ~4x/sec.
/// </summary>
public readonly record struct EngineStatus(
    bool Running,
    bool Receiving,
    bool IsRaceOn,
    int PacketsPerSecond);

/// <summary>
/// The fan-out relay. Binds the UDP port Forza's "Data Out" targets, then resends every
/// received datagram, byte-for-byte, to each enabled <see cref="Destination"/>.
///
/// Design notes (see plan):
///  - A single blocking receive loop on a dedicated thread. Volume is tiny (~60-120 pkt/s),
///    and a plain loop has lower, more predictable latency than async/threadpool fan-out.
///  - No parsing on the hot path: we forward raw bytes and only peek byte 0 for IsRaceOn.
///  - One source socket, many SendTo() calls per packet.
/// </summary>
public sealed class SplitterEngine
{
    private readonly object _gate = new();
    private Socket? _socket;
    private Thread? _rxThread;
    private volatile bool _running;

    private List<Destination> _destinations = new();

    // Status fields written on the rx thread, read by the UI timer.
    private long _lastPacketTicks;          // Stopwatch ticks of the last valid packet
    private volatile bool _lastIsRaceOn;
    private int _packetsThisSecond;         // accumulator
    private int _packetsPerSecond;          // last completed 1s window
    private long _windowStartTicks;

    /// <summary>Raised (on the rx thread) when binding fails or the socket dies unexpectedly.</summary>
    public event Action<string>? ErrorOccurred;

    public bool Running => _running;

    /// <summary>
    /// Replace the active destination list. Safe to call while running; the rx loop reads
    /// the reference atomically each packet, so swapping the list takes effect immediately.
    /// </summary>
    public void SetDestinations(IEnumerable<Destination> destinations)
    {
        var list = destinations.ToList();
        lock (_gate) { _destinations = list; }
    }

    /// <summary>
    /// Bind and start the receive loop. Returns true on success; on failure (e.g. the port is
    /// already taken by another running splitter or by VirtualTCU still on 5555) returns false
    /// and raises <see cref="ErrorOccurred"/> with a human-readable explanation.
    /// </summary>
    public bool Start(string listenIp, int listenPort)
    {
        lock (_gate)
        {
            if (_running) return true;

            try
            {
                if (!IPAddress.TryParse(listenIp, out var bindAddr))
                    bindAddr = IPAddress.Loopback;

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // Larger receive buffer so a brief UI hiccup never drops Forza packets.
                socket.ReceiveBufferSize = 1 << 20; // 1 MB
                socket.Bind(new IPEndPoint(bindAddr, listenPort));

                _socket = socket;
                _running = true;
                _windowStartTicks = Stopwatch.GetTimestamp();

                _rxThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "ForzaSplitter-Rx",
                    Priority = ThreadPriority.AboveNormal,
                };
                _rxThread.Start();
                return true;
            }
            catch (SocketException ex) when (
                ex.SocketErrorCode == SocketError.AddressAlreadyInUse ||   // WSAEADDRINUSE (10048)
                ex.SocketErrorCode == SocketError.AccessDenied)            // WSAEACCES   (10013)
            {
                // Both codes mean "something else already owns this port". Windows reports an
                // exclusively-bound UDP port (e.g. VirtualTCU on 5555) as AccessDenied, not
                // AddressAlreadyInUse — so we must treat them the same.
                _running = false;
                ErrorOccurred?.Invoke(
                    $"Port {listenPort} is already in use by another app.\n\n" +
                    "Forza Telemetry Splitter needs its own free port to receive from Forza. " +
                    "Another tool (or a second copy of the splitter) is holding this one.\n\n" +
                    $"Fix: open the splitter and change its listen port to a free one, then set " +
                    $"Forza's Data Out port to match. The default {listenPort} is normally free.");
                return false;
            }
            catch (Exception ex)
            {
                _running = false;
                ErrorOccurred?.Invoke($"Could not start the splitter on {listenIp}:{listenPort}.\n\n{ex.Message}");
                return false;
            }
        }
    }

    /// <summary>Stop the loop and release the socket. Safe to call when already stopped.</summary>
    public void Stop()
    {
        Thread? rx;
        Socket? sock;
        lock (_gate)
        {
            if (!_running) return;
            _running = false;
            sock = _socket;
            _socket = null;
            rx = _rxThread;
            _rxThread = null;
        }

        // Closing the socket unblocks the blocking Receive() in the loop.
        try { sock?.Close(); } catch { /* ignore */ }
        try { rx?.Join(500); } catch { /* ignore */ }

        _packetsPerSecond = 0;
        _packetsThisSecond = 0;
    }

    private void ReceiveLoop()
    {
        var buffer = new byte[2048]; // comfortably larger than the 324-byte Car Dash packet
        var socket = _socket;
        if (socket is null) return;

        while (_running)
        {
            int received;
            try
            {
                received = socket.Receive(buffer);
            }
            catch (SocketException) when (!_running)
            {
                break; // expected: socket closed by Stop()
            }
            catch (ObjectDisposedException)
            {
                break; // expected on shutdown
            }
            catch (SocketException ex)
            {
                if (_running) ErrorOccurred?.Invoke($"Receive error: {ex.Message}");
                break;
            }

            // Forward first (latency-critical), account for status second.
            var span = new ReadOnlySpan<byte>(buffer, 0, received);

            List<Destination> dests;
            lock (_gate) { dests = _destinations; }

            for (int i = 0; i < dests.Count; i++)
            {
                var d = dests[i];
                if (!d.Enabled) continue;
                var ep = d.GetEndPoint();
                if (ep is null) continue;
                try
                {
                    socket.SendTo(buffer, 0, received, SocketFlags.None, ep);
                    Interlocked.Increment(ref d.ForwardedCount);
                }
                catch (SocketException)
                {
                    // A downstream tool that isn't listening yet just yields a transient error
                    // on localhost. Ignore — it will start receiving once it binds its port.
                }
            }

            UpdateStatus(span);
        }
    }

    private void UpdateStatus(ReadOnlySpan<byte> packet)
    {
        if (!ForzaPacket.IsCarDash(packet.Length))
            return; // ignore non-Forza traffic for status purposes

        _lastIsRaceOn = ForzaPacket.IsRaceOn(packet);
        Volatile.Write(ref _lastPacketTicks, Stopwatch.GetTimestamp());

        _packetsThisSecond++;
        long now = Stopwatch.GetTimestamp();
        if (now - _windowStartTicks >= Stopwatch.Frequency)
        {
            _packetsPerSecond = _packetsThisSecond;
            _packetsThisSecond = 0;
            _windowStartTicks = now;
        }
    }

    /// <summary>
    /// Current status snapshot for the UI. "Receiving" = a valid Forza packet seen within
    /// the last second.
    /// </summary>
    public EngineStatus GetStatus()
    {
        long last = Volatile.Read(ref _lastPacketTicks);
        bool receiving = false;
        if (last != 0)
        {
            double secondsSince = (Stopwatch.GetTimestamp() - last) / (double)Stopwatch.Frequency;
            receiving = secondsSince < 1.0;
        }

        // If we haven't seen a packet in over a second, pkts/s is effectively 0.
        int pps = receiving ? _packetsPerSecond : 0;

        return new EngineStatus(
            Running: _running,
            Receiving: receiving,
            IsRaceOn: receiving && _lastIsRaceOn,
            PacketsPerSecond: pps);
    }
}
