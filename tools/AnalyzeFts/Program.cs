using System.Text;
using ForzaTelemetrySplitter.Core;

// Parse and analyze a .fts capture for issues: timing/gaps, packet-rate stability, format
// consistency, and telemetry sanity (IsRaceOn, gear, speed).
//
// .fts format: header "FTS1" + int32 version(1); then per packet:
//   int64 deltaTicks (Stopwatch ticks since previous), uint16 length, then <length> bytes.
// Stopwatch frequency isn't stored, so we infer real time from the producer's machine: ticks are
// System.Diagnostics.Stopwatch ticks. We can't know the exact frequency from the file, but on Windows
// it's typically 10,000,000 (100ns) — we detect/assume that and note the assumption.

if (args.Length >= 1 && args[0] == "--fidelity")
{
    Console.WriteLine("=== Recorder fidelity test (real engine + recorder) ===");
    return Fidelity.Run();
}
if (args.Length >= 2 && args[0] == "--gears")
{
    return GearProbe.Run(args[1]);
}
if (args.Length < 1) { Console.WriteLine("usage: AnalyzeFts <file.fts> | --fidelity"); return 1; }
string path = args[0];
if (!File.Exists(path)) { Console.WriteLine($"not found: {path}"); return 1; }

using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
using var br = new BinaryReader(fs);

var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
int version = br.ReadInt32();
Console.WriteLine($"File: {Path.GetFileName(path)}  ({fs.Length:N0} bytes)");
Console.WriteLine($"Header: magic='{magic}' version={version}");
if (magic != "FTS1") { Console.WriteLine("  WARNING: unexpected magic"); }

const double tickHz = 10_000_000.0; // Stopwatch ticks/sec on Windows (assumed)

long count = 0;
var deltasMs = new List<double>();
var lengths = new Dictionary<int, long>();
long raceOnCount = 0, raceOffCount = 0;
var gearHist = new Dictionary<int, long>();
float maxSpeedMps = 0, sumSpeed = 0; long speedSamples = 0;
double totalSeconds = 0;
long bigGaps = 0;           // > 500 ms between packets
double maxGapMs = 0;
long shortPackets = 0, unknownFormat = 0;
ForzaFormat fmt = ForzaFormat.Unknown;
int lastGear = int.MinValue; long gearChanges = 0;

while (fs.Position < fs.Length)
{
    long deltaTicks;
    int len;
    try { deltaTicks = br.ReadInt64(); len = br.ReadUInt16(); }
    catch (EndOfStreamException) { Console.WriteLine("  WARNING: file truncated mid-record"); break; }

    if (fs.Position + len > fs.Length) { Console.WriteLine("  WARNING: truncated payload at end"); break; }
    var bytes = br.ReadBytes(len);
    count++;

    double ms = deltaTicks / tickHz * 1000.0;
    if (count > 1) { deltasMs.Add(ms); totalSeconds += ms / 1000.0; if (ms > maxGapMs) maxGapMs = ms; if (ms > 500) bigGaps++; }

    lengths[len] = lengths.GetValueOrDefault(len) + 1;
    var f = ForzaPacket.Detect(len);
    if (f == ForzaFormat.Unknown) unknownFormat++; else fmt = f;
    if (len < 232) { shortPackets++; continue; }

    if (ForzaPacket.IsRaceOn(bytes)) raceOnCount++; else raceOffCount++;

    if (f is ForzaFormat.HorizonCarDash or ForzaFormat.MotorsportDash)
    {
        int gear = ForzaPacket.Gear(bytes, f);
        gearHist[gear] = gearHist.GetValueOrDefault(gear) + 1;
        if (lastGear != int.MinValue && gear != lastGear) gearChanges++;
        lastGear = gear;

        float sp = ForzaPacket.SpeedMetersPerSecond(bytes, f);
        if (sp >= 0 && sp < 200) { sumSpeed += sp; speedSamples++; if (sp > maxSpeedMps) maxSpeedMps = sp; }
    }
}

Console.WriteLine($"\nPackets: {count:N0}");
Console.WriteLine($"Detected game format: {ForzaPacket.GameName(fmt)} ({fmt})");
Console.WriteLine($"Packet sizes seen: {string.Join(", ", lengths.OrderByDescending(k => k.Value).Select(k => $"{k.Key}B x{k.Value}"))}");
if (unknownFormat > 0) Console.WriteLine($"  WARNING: {unknownFormat} packets had an UNKNOWN size (not a recognized Forza format)");
if (shortPackets > 0) Console.WriteLine($"  WARNING: {shortPackets} packets shorter than 232B (corrupt/partial?)");

if (deltasMs.Count > 0)
{
    deltasMs.Sort();
    double avg = deltasMs.Average();
    double p50 = Pct(deltasMs, 50), p99 = Pct(deltasMs, 99), max = deltasMs[^1];
    double hz = avg > 0 ? 1000.0 / avg : 0;
    Console.WriteLine($"\nTiming (inter-packet, assuming {tickHz/1e6:N0}MHz tick):");
    Console.WriteLine($"  duration: {totalSeconds:N1} s ({TimeSpan.FromSeconds(totalSeconds):mm\\:ss})");
    Console.WriteLine($"  avg gap: {avg:F2} ms  -> ~{hz:F0} packets/sec");
    Console.WriteLine($"  p50 {p50:F2} ms | p99 {p99:F2} ms | max {max:F1} ms");
    Console.WriteLine($"  gaps >500ms: {bigGaps}  (largest {maxGapMs:F0} ms)");
    // Expected Forza rate is ~60 Hz (16.7ms). Flag if average is way off.
    if (hz < 40 || hz > 70) Console.WriteLine($"  NOTE: packet rate ~{hz:F0}/s is outside the usual ~60/s for Forza");
}

Console.WriteLine($"\nTelemetry sanity:");
Console.WriteLine($"  IsRaceOn: ON={raceOnCount:N0}  OFF={raceOffCount:N0}");
if (raceOnCount == 0) Console.WriteLine("  NOTE: no 'race on' packets — capture may be all menu/paused");
if (speedSamples > 0)
    Console.WriteLine($"  speed: avg {sumSpeed/speedSamples*2.236936:F0} mph, max {maxSpeedMps*2.236936:F0} mph ({maxSpeedMps:F1} m/s)");
if (gearHist.Count > 0)
    Console.WriteLine($"  gears seen: {string.Join(", ", gearHist.OrderBy(k => k.Key).Select(k => $"G{k.Key}:{k.Value}"))}  ({gearChanges} shifts)");

// Heuristic issue summary.
Console.WriteLine("\n=== Issue summary ===");
int issues = 0;
if (unknownFormat > 0) { Console.WriteLine($"- {unknownFormat} unrecognized-size packets"); issues++; }
if (shortPackets > 0) { Console.WriteLine($"- {shortPackets} too-short packets"); issues++; }
if (bigGaps > 0) { Console.WriteLine($"- {bigGaps} gaps >500ms (possible dropouts/pauses; largest {maxGapMs:F0}ms)"); issues++; }
if (deltasMs.Count > 0 && Pct(deltasMs, 99) > 50) { Console.WriteLine($"- p99 inter-packet {Pct(deltasMs,99):F0}ms is high (jitter or hitching)"); issues++; }
if (issues == 0) Console.WriteLine("- none detected");
return 0;

static double Pct(List<double> s, double p)
{
    if (s.Count == 0) return 0;
    double r = p / 100.0 * (s.Count - 1);
    int lo = (int)Math.Floor(r), hi = (int)Math.Ceiling(r);
    return lo == hi ? s[lo] : s[lo] + (r - lo) * (s[hi] - s[lo]);
}
