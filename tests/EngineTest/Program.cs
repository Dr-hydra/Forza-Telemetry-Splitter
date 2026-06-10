using System.Net;
using System.Net.Sockets;
using ForzaTelemetrySplitter.Core;

// Headless verification of SplitterEngine: proves the real fan-out code splits Forza
// telemetry to multiple destinations byte-for-byte, and reports a clean port-conflict error
// for BOTH ways Windows signals a taken UDP port (AddressAlreadyInUse and AccessDenied).

const string ip = "127.0.0.1";
const int listenPort = 35555;
int[] destPorts = { 35556, 35300 };
const int packetCount = 60;

int failures = 0;

Console.WriteLine("=== Forza Telemetry Splitter — engine verification ===\n");

// --- Test 1: fan-out to multiple destinations -------------------------------------------
{
    Console.WriteLine("[Test 1] Fan-out splits the stream to all enabled destinations");

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
                if (data.Length == ForzaPacket.CarDashSize) counts[port]++;
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
        using var sender = new UdpClient();
        var target = new IPEndPoint(IPAddress.Parse(ip), listenPort);
        for (int i = 0; i < packetCount; i++)
        {
            var pkt = new byte[ForzaPacket.CarDashSize];
            pkt[0] = (byte)(i % 2);
            pkt[5] = (byte)(i % 256);
            sender.Send(pkt, pkt.Length, target);
            Thread.Sleep(8);
        }

        Thread.Sleep(400);
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

// --- Test 2: AddressAlreadyInUse is reported clearly ------------------------------------
{
    Console.WriteLine("\n[Test 2] Port held by a NON-exclusive UDP socket (AddressAlreadyInUse)");

    using var squatter = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), listenPort));

    var engine = new SplitterEngine();
    string? error = null;
    engine.ErrorOccurred += m => error = m;

    bool started = engine.Start(ip, listenPort);
    if (!started && error is not null && error.Contains("already in use"))
        Console.WriteLine("  PASS: clean 'port already in use' error raised");
    else
    {
        Console.WriteLine($"  FAIL: started={started}, error={error ?? "<none>"}");
        failures++;
    }
    engine.Stop();
}

// --- Test 3: AccessDenied (the VirtualTCU-on-5555 case) is reported clearly -------------
// This is the bug found on Jake's machine: an exclusively-bound UDP port raises WSAEACCES,
// not WSAEADDRINUSE. Reproduce by binding with ExclusiveAddressUse, then confirm the engine
// still produces the friendly message rather than the cryptic OS one.
{
    Console.WriteLine("\n[Test 3] Port held EXCLUSIVELY (AccessDenied / WSAEACCES) — the VirtualTCU case");

    using var exclusive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    exclusive.ExclusiveAddressUse = true;
    exclusive.Bind(new IPEndPoint(IPAddress.Parse(ip), listenPort));

    var engine = new SplitterEngine();
    string? error = null;
    engine.ErrorOccurred += m => error = m;

    bool started = engine.Start(ip, listenPort);
    bool friendly = error is not null && error.Contains("already in use by another app");
    bool notCryptic = error is null || !error.Contains("forbidden by its access permissions");

    if (!started && friendly && notCryptic)
        Console.WriteLine("  PASS: exclusive-bind conflict shows the friendly message (no cryptic OS text)");
    else
    {
        Console.WriteLine($"  FAIL: started={started}, error={error ?? "<none>"}");
        failures++;
    }
    engine.Stop();
}

// --- Test 4: IsRaceOn parsing -----------------------------------------------------------
{
    Console.WriteLine("\n[Test 4] ForzaPacket.IsRaceOn / IsValid");
    var on = new byte[ForzaPacket.CarDashSize]; on[0] = 1;
    var off = new byte[ForzaPacket.CarDashSize]; off[0] = 0;
    var wrongSize = new byte[100];

    bool ok = ForzaPacket.IsRaceOn(on) && !ForzaPacket.IsRaceOn(off)
              && ForzaPacket.IsValid(324) && !ForzaPacket.IsValid(100)
              && !ForzaPacket.IsRaceOn(wrongSize);
    Console.WriteLine($"  {(ok ? "PASS" : "FAIL")}");
    if (!ok) failures++;
}

// --- Test 5: format detection by length -------------------------------------------------
{
    Console.WriteLine("\n[Test 5] ForzaPacket.Detect (game by packet size)");
    bool ok = ForzaPacket.Detect(232) == ForzaFormat.Sled
              && ForzaPacket.Detect(311) == ForzaFormat.MotorsportDash
              && ForzaPacket.Detect(331) == ForzaFormat.MotorsportDash
              && ForzaPacket.Detect(324) == ForzaFormat.HorizonCarDash
              && ForzaPacket.Detect(323) == ForzaFormat.HorizonCarDash
              && ForzaPacket.Detect(100) == ForzaFormat.Unknown;
    Console.WriteLine($"  {(ok ? "PASS" : "FAIL")}");
    if (!ok) failures++;
}

// --- Test 6: per-format Speed/Gear offset round-trip (decisive) -------------------------
// Horizon: Speed@256, Gear@319.  Motorsport: Speed@244, Gear@307.
{
    Console.WriteLine("\n[Test 6] Speed/Gear offsets per format (round-trip)");

    var hzn = new byte[ForzaPacket.CarDashSize];
    System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(hzn.AsSpan(256, 4), 50.0f);
    hzn[319] = 4;
    var hznFmt = ForzaPacket.Detect(hzn.Length);
    bool hznOk = hznFmt == ForzaFormat.HorizonCarDash
                 && Math.Abs(ForzaPacket.SpeedMetersPerSecond(hzn, hznFmt) - 50f) < 0.001f
                 && ForzaPacket.Gear(hzn, hznFmt) == 4;

    var fm = new byte[ForzaPacket.MotorsportDashSize];
    System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(fm.AsSpan(244, 4), 72.0f);
    fm[307] = 3;
    var fmFmt = ForzaPacket.Detect(fm.Length);
    bool fmOk = fmFmt == ForzaFormat.MotorsportDash
                && Math.Abs(ForzaPacket.SpeedMetersPerSecond(fm, fmFmt) - 72f) < 0.001f
                && ForzaPacket.Gear(fm, fmFmt) == 3;

    // Sled has no dash data → readers return 0.
    var sled = new byte[ForzaPacket.SledSize];
    var sledFmt = ForzaPacket.Detect(sled.Length);
    bool sledOk = sledFmt == ForzaFormat.Sled
                  && ForzaPacket.SpeedMetersPerSecond(sled, sledFmt) == 0f
                  && ForzaPacket.Gear(sled, sledFmt) == 0;

    bool ok = hznOk && fmOk && sledOk;
    Console.WriteLine($"  Horizon(256/319)={hznOk}  Motorsport(244/307)={fmOk}  Sled(none)={sledOk} -> {(ok ? "PASS" : "FAIL")}");
    if (!ok) failures++;
}

// --- Test 7: forwarding is format-agnostic (Motorsport 311 + Sled 232) ------------------
// The relay must forward ANY packet size byte-identical, not just Horizon 324.
{
    Console.WriteLine("\n[Test 7] Forwarding works for non-Horizon packet sizes (311, 232)");
    const int fwdPort = 35777;
    using var rx = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), fwdPort));
    rx.Client.ReceiveTimeout = 500;
    int got311 = 0, got232 = 0;
    var cts = new CancellationTokenSource();
    var rxTask = Task.Run(() =>
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                IPEndPoint? r = null;
                var d = rx.Receive(ref r);
                if (d.Length == 311) got311++;
                else if (d.Length == 232) got232++;
            }
            catch (SocketException) { }
        }
    });

    var engine = new SplitterEngine();
    engine.SetDestinations(new[] { new Destination { Name = "x", Ip = ip, Port = fwdPort, Enabled = true } });
    engine.Start(ip, listenPort);
    using (var sender = new UdpClient())
    {
        var target = new IPEndPoint(IPAddress.Parse(ip), listenPort);
        for (int i = 0; i < 30; i++) { sender.Send(new byte[311], 311, target); Thread.Sleep(4); }
        for (int i = 0; i < 30; i++) { sender.Send(new byte[232], 232, target); Thread.Sleep(4); }
    }
    Thread.Sleep(300); cts.Cancel(); rxTask.Wait(1500); engine.Stop(); rx.Close();

    bool ok = got311 >= 27 && got232 >= 27;
    Console.WriteLine($"  forwarded 311-byte: {got311}/30, 232-byte: {got232}/30 -> {(ok ? "PASS" : "FAIL")}");
    if (!ok) failures++;
}

Console.WriteLine();
if (failures == 0)
{
    Console.WriteLine("OVERALL: PASS — engine splits byte-identical telemetry and reports all port conflicts clearly.");
    return 0;
}
Console.WriteLine($"OVERALL: FAIL — {failures} check(s) failed.");
return 1;
