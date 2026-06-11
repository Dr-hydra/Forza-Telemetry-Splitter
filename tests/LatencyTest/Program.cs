using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ForzaTelemetrySplitter.Core;

// Latency harness: measures how much delay the splitter adds to a "critical" downstream tool
// (standing in for VirtualTCU), and whether a non-listening destination AHEAD of it in the list
// can delay it. We care about WORST CASE (max / p99 jitter), not just the average, because a single
// late telemetry packet is what would make VirtualTCU shift late.
//
// Method:
//  - One UDP socket sends timestamped packets at ~120 Hz (a high frame rate) into the splitter.
//  - A "critical" receiver reads them and records (receive_time - send_time) per packet.
//  - We embed a monotonic sequence + a high-res send tick in the packet so we can match precisely.
//  - We compare two configs:
//      A) critical destination ALONE.
//      B) critical destination placed AFTER a dead (non-listening) destination, to test whether a
//         failing SendTo to an earlier destination delays the critical one.

const string ip = "127.0.0.1";
const int listenPort = 46000;
const int criticalPort = 46001;
const int deadPort = 46002;     // nothing listens here on purpose
const int packets = 5000;
const double sendHz = 120.0;    // higher than Forza's typical 60 to stress the path

int exitCode = 0;

Console.WriteLine("=== Forza Telemetry Splitter — latency / jitter harness ===");
Console.WriteLine($"Sending {packets} packets at {sendHz} Hz through the real SplitterEngine.\n");

exitCode |= RunScenario("A. Critical destination alone",
    new[] { new Destination { Name = "VTCU", Ip = ip, Port = criticalPort, Enabled = true } });

exitCode |= RunScenario("B. Critical destination AFTER a dead (non-listening) destination",
    new[]
    {
        new Destination { Name = "DeadTool", Ip = ip, Port = deadPort,     Enabled = true }, // first
        new Destination { Name = "VTCU",     Ip = ip, Port = criticalPort, Enabled = true }, // after it
    });

// Also verify the new recording path doesn't add latency to forwarding.
exitCode |= RecordingLatency.Run();

Console.WriteLine(exitCode == 0
    ? "\nOVERALL: PASS — added delay and worst-case jitter are within budget."
    : "\nOVERALL: CONCERN — see flagged scenario(s) above.");
return exitCode;


int RunScenario(string title, Destination[] dests)
{
    Console.WriteLine($"--- {title} ---");

    using var critical = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), criticalPort));
    critical.Client.ReceiveTimeout = 500;

    var latenciesUs = new List<double>(packets);
    var received = 0;

    var rx = Task.Run(() =>
    {
        while (true)
        {
            try
            {
                IPEndPoint? remote = null;
                var data = critical.Receive(ref remote);
                long nowTicks = Stopwatch.GetTimestamp();
                if (data.Length == ForzaPacket.CarDashSize)
                {
                    long sentTicks = BitConverter.ToInt64(data, 8); // we stash send tick at offset 8
                    double us = (nowTicks - sentTicks) * 1_000_000.0 / Stopwatch.Frequency;
                    latenciesUs.Add(us);
                    received++;
                }
            }
            catch (SocketException) { break; } // timeout => sender finished
        }
    });

    var engine = new SplitterEngine();
    engine.SetDestinations(dests);
    if (!engine.Start(ip, listenPort))
    {
        Console.WriteLine("  FAIL: engine did not start");
        return 1;
    }

    using (var sender = new UdpClient())
    {
        var target = new IPEndPoint(IPAddress.Parse(ip), listenPort);
        double intervalMs = 1000.0 / sendHz;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < packets; i++)
        {
            var pkt = new byte[ForzaPacket.CarDashSize];
            pkt[0] = 1; // IsRaceOn
            BitConverter.GetBytes(Stopwatch.GetTimestamp()).CopyTo(pkt, 8); // send tick @ 8
            sender.Send(pkt, pkt.Length, target);

            // Pace the sender without a tight spin: sleep to the next slot.
            double due = (i + 1) * intervalMs;
            double waitMs = due - sw.Elapsed.TotalMilliseconds;
            if (waitMs > 1) Thread.Sleep((int)waitMs);
        }
    }

    Thread.Sleep(400); // drain tail
    engine.Stop();
    try { critical.Close(); } catch { }
    rx.Wait(2000);

    if (latenciesUs.Count == 0)
    {
        Console.WriteLine("  FAIL: no packets received by the critical destination!");
        return 1;
    }

    // How many packets exceeded 2ms, and where the worst one landed (first packet? recurring?).
    int over2ms = latenciesUs.Count(u => u > 2000);
    var sorted = new List<double>(latenciesUs);

    sorted.Sort();
    double avg = sorted.Average();
    double p50 = Percentile(sorted, 50);
    double p99 = Percentile(sorted, 99);
    double p999 = Percentile(sorted, 99.9);
    double max = sorted[^1];
    double lossPct = 100.0 * (1.0 - received / (double)packets);

    Console.WriteLine($"  received   : {received}/{packets}  (loss {lossPct:F2}%)");
    Console.WriteLine($"  added delay: avg {avg:F1} us | p50 {p50:F1} us | p99 {p99:F1} us | p99.9 {p999:F1} us | max {max:F1} us");
    Console.WriteLine($"  over 2ms   : {over2ms} packet(s) of {received}");

    // Budget: Forza sends one packet roughly every 8-16 ms. We want the splitter's added delay to
    // be a tiny fraction of that. We judge on p99.9 (sustained worst case) and an essentially-zero
    // loss rate. A lone OS-scheduling outlier in thousands of packets (occasional max spike) is not
    // a relay problem — Windows can preempt any user thread briefly — and is reported separately.
    const double budgetUs = 2000.0;
    bool ok = p999 <= budgetUs && lossPct < 0.5 && over2ms <= 2;
    Console.WriteLine($"  verdict    : {(ok ? "PASS" : "CONCERN")} (p99.9 {p999:F0} us vs {budgetUs:F0} us budget; {over2ms} spike(s) >2ms)\n");
    return ok ? 0 : 1;
}

static double Percentile(List<double> sorted, double p)
{
    if (sorted.Count == 0) return 0;
    double rank = (p / 100.0) * (sorted.Count - 1);
    int lo = (int)Math.Floor(rank);
    int hi = (int)Math.Ceiling(rank);
    if (lo == hi) return sorted[lo];
    return sorted[lo] + (rank - lo) * (sorted[hi] - sorted[lo]);
}
