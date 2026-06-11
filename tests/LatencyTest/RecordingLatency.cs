using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ForzaTelemetrySplitter.Core;

// Focused check: does ACTIVE RECORDING add latency to forwarding? Recording writes each packet to a
// .fts file inside the receive loop (after forwarding), so we measure the critical destination's
// added delay with recording ON vs OFF and assert recording doesn't materially worsen it.
//
// Invoked from Program.cs via RecordingLatency.Run(). Returns 0 on pass.
internal static class RecordingLatency
{
    public static int Run()
    {
        const string ip = "127.0.0.1";
        const int listenPort = 47000;
        const int criticalPort = 47001;
        const int packets = 4000;
        const double hz = 120.0;

        Console.WriteLine("\n=== Recording-active latency check ===");

        double offP999 = Measure(ip, listenPort, criticalPort, packets, hz, record: false, out int offSpikes);
        double onP999 = Measure(ip, listenPort, criticalPort, packets, hz, record: true, out int onSpikes);

        Console.WriteLine($"  recording OFF: p99.9 {offP999:F0} us, spikes>2ms {offSpikes}");
        Console.WriteLine($"  recording ON : p99.9 {onP999:F0} us, spikes>2ms {onSpikes}");

        // Pass if recording-on p99.9 stays within budget AND doesn't blow past off by a wide margin.
        const double budgetUs = 2000.0;
        bool ok = onP999 <= budgetUs && onSpikes <= 3 && onP999 <= Math.Max(offP999 * 3, 800);
        Console.WriteLine($"  verdict: {(ok ? "PASS" : "CONCERN")} (recording must not materially add latency)\n");
        return ok ? 0 : 1;
    }

    private static double Measure(string ip, int listenPort, int criticalPort, int packets, double hz,
                                  bool record, out int spikesOver2ms)
    {
        using var critical = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), criticalPort));
        critical.Client.ReceiveTimeout = 500;
        var latUs = new List<double>(packets);
        var cts = new CancellationTokenSource();
        var rx = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint? r = null;
                    var d = critical.Receive(ref r);
                    long now = Stopwatch.GetTimestamp();
                    if (d.Length == ForzaPacket.CarDashSize)
                    {
                        long sent = BitConverter.ToInt64(d, 8);
                        latUs.Add((now - sent) * 1_000_000.0 / Stopwatch.Frequency);
                    }
                }
                catch (SocketException) { break; }
            }
        });

        var engine = new SplitterEngine();
        engine.SetDestinations(new[] { new Destination { Name = "VTCU", Ip = ip, Port = criticalPort, Enabled = true } });
        engine.Start(ip, listenPort);

        string? recPath = null;
        if (record)
        {
            recPath = Path.Combine(Path.GetTempPath(), $"lat-rec-{Guid.NewGuid():N}.fts");
            engine.StartRecording(recPath);
        }

        using (var sender = new UdpClient())
        {
            var target = new IPEndPoint(IPAddress.Parse(ip), listenPort);
            double intervalMs = 1000.0 / hz;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < packets; i++)
            {
                var pkt = new byte[ForzaPacket.CarDashSize];
                pkt[0] = 1;
                BitConverter.GetBytes(Stopwatch.GetTimestamp()).CopyTo(pkt, 8);
                sender.Send(pkt, pkt.Length, target);
                double due = (i + 1) * intervalMs;
                double wait = due - sw.Elapsed.TotalMilliseconds;
                if (wait > 1) Thread.Sleep((int)wait);
            }
        }

        Thread.Sleep(300);
        cts.Cancel();
        engine.Stop(); // also stops recording
        rx.Wait(2000);
        try { critical.Close(); } catch { }
        if (recPath is not null) { try { File.Delete(recPath); } catch { } }

        spikesOver2ms = latUs.Count(u => u > 2000);
        latUs.Sort();
        return Percentile(latUs, 99.9);
    }

    private static double Percentile(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        double rank = p / 100.0 * (sorted.Count - 1);
        int lo = (int)Math.Floor(rank), hi = (int)Math.Ceiling(rank);
        return lo == hi ? sorted[lo] : sorted[lo] + (rank - lo) * (sorted[hi] - sorted[lo]);
    }
}
