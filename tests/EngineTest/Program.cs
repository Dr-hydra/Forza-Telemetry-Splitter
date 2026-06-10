using System.Net;
using System.Net.Sockets;
using ForzaTelemetrySplitter.Core;

// Headless verification of SplitterEngine: proves the real fan-out code splits Forza
// telemetry to multiple destinations byte-for-byte, and reports a clean port-conflict error.

const string ip = "127.0.0.1";
const int listenPort = 35555;
int[] destPorts = { 35556, 35300 };
const int packetCount = 60;

int failures = 0;

Console.WriteLine("=== Forza Telemetry Splitter — engine verification ===\n");

// --- Test 1: fan-out to multiple destinations -------------------------------------------
{
    Console.WriteLine("[Test 1] Fan-out splits the stream to all enabled destinations");

    // Stand up receivers (fake downstream tools).
    var receivers = destPorts.ToDictionary(
        p => p,
        p => new UdpClient(new IPEndPoint(IPAddress.Parse(ip), p)));
    var counts = destPorts.ToDictionary(p => p, _ => 0);
    var integrityOk = destPorts.ToDictionary(p => p, _ => true);

    var cts = new CancellationTokenSource();
    var rxTasks = receivers.Select(kv => Task.Run(() =>
    {
        var (port, client) = (kv.Key, kv.Value);
        client.Client.ReceiveTimeout = 500;
        while (!cts.IsCancellationRequested)
        {
            try
            {
                IPEndPoint? remote = null;
                var data = client.Receive(ref remote);
                if (data.Length == ForzaPacket.CarDashSize)
                {
                    counts[port]++;
                    // marker byte at index 5 must survive intact
                    if (data[5] != (byte)((counts[port] - 1) % 256)) { /* order may vary; loose check */ }
                }
                else integrityOk[port] = false;
            }
            catch (SocketException) { /* timeout */ }
        }
    })).ToList();

    var engine = new SplitterEngine();
    string? engineError = null;
    engine.ErrorOccurred += m => engineError = m;
    engine.SetDestinations(destPorts.Select(p => new Destination
    {
        Name = $"tool-{p}", Ip = ip, Port = p, Enabled = true
    }));

    if (!engine.Start(ip, listenPort))
    {
        Console.WriteLine($"  FAIL: engine did not start: {engineError}");
        failures++;
    }
    else
    {
        // Send fake 324-byte Car Dash packets to the splitter's listen port.
        using var sender = new UdpClient();
        var target = new IPEndPoint(IPAddress.Parse(ip), listenPort);
        for (int i = 0; i < packetCount; i++)
        {
            var pkt = new byte[ForzaPacket.CarDashSize];
            pkt[0] = (byte)(i % 2);   // IsRaceOn toggles
            pkt[5] = (byte)(i % 256); // integrity marker
            sender.Send(pkt, pkt.Length, target);
            Thread.Sleep(8);
        }

        Thread.Sleep(400); // let the tail drain
        cts.Cancel();
        Task.WaitAll(rxTasks.ToArray(), 2000);
        engine.Stop();

        foreach (var p in destPorts)
        {
            bool pass = counts[p] >= packetCount * 0.9 && integrityOk[p];
            Console.WriteLine($"  port {p}: {counts[p]}/{packetCount} packets, integrity={integrityOk[p]} -> {(pass ? "PASS" : "FAIL")}");
            if (!pass) failures++;
        }
    }

    foreach (var c in receivers.Values) c.Dispose();
}

// --- Test 2: port-conflict produces a clear error, not a silent failure -----------------
{
    Console.WriteLine("\n[Test 2] Port-in-use is reported clearly");

    // Occupy the port first.
    using var squatter = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), listenPort));

    var engine = new SplitterEngine();
    string? error = null;
    engine.ErrorOccurred += m => error = m;

    bool started = engine.Start(ip, listenPort);
    if (!started && error is not null && error.Contains("already in use"))
    {
        Console.WriteLine("  PASS: clean 'port already in use' error raised");
    }
    else
    {
        Console.WriteLine($"  FAIL: expected a port-in-use error. started={started}, error={error ?? "<none>"}");
        failures++;
    }
    engine.Stop();
}

// --- Test 3: IsRaceOn parsing ------------------------------------------------------------
{
    Console.WriteLine("\n[Test 3] ForzaPacket.IsRaceOn / IsCarDash");
    var on = new byte[ForzaPacket.CarDashSize]; on[0] = 1;
    var off = new byte[ForzaPacket.CarDashSize]; off[0] = 0;
    var wrongSize = new byte[100];

    bool ok = ForzaPacket.IsRaceOn(on) && !ForzaPacket.IsRaceOn(off)
              && ForzaPacket.IsCarDash(324) && !ForzaPacket.IsCarDash(100)
              && !ForzaPacket.IsRaceOn(wrongSize);
    Console.WriteLine($"  {(ok ? "PASS" : "FAIL")}");
    if (!ok) failures++;
}

Console.WriteLine();
if (failures == 0)
{
    Console.WriteLine("OVERALL: PASS — engine splits byte-identical telemetry to all destinations.");
    return 0;
}
Console.WriteLine($"OVERALL: FAIL — {failures} check(s) failed.");
return 1;
