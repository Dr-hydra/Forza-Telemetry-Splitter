// Probe what gear values mean in a real .fts: correlate each gear with speed + IsRaceOn so we can
// tell empirically whether 0=reverse, 11=neutral, etc.
//
// Run: dotnet run --project tools/AnalyzeFts -c Release -- --gears <file.fts>

using System.Text;
using ForzaTelemetrySplitter.Core;

internal static class GearProbe
{
    public static int Run(string path)
    {
        if (!File.Exists(path)) { Console.WriteLine($"not found: {path}"); return 1; }
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        br.ReadBytes(4); br.ReadInt32();

        // gear -> (count, sumSpeedMph, raceOn, sample of preceding/following gear)
        var stats = new Dictionary<int, (long n, double sumSpeed, long raceOn, double maxSpeed)>();
        int prevGear = int.MinValue;
        var transitionsFrom11 = new Dictionary<int, long>(); // what gear 11 is followed by

        while (fs.Position < fs.Length)
        {
            long dt; int len;
            try { dt = br.ReadInt64(); len = br.ReadUInt16(); } catch { break; }
            if (fs.Position + len > fs.Length) break;
            var b = br.ReadBytes(len);
            var f = ForzaPacket.Detect(len);
            if (f is not (ForzaFormat.HorizonCarDash or ForzaFormat.MotorsportDash)) continue;

            int g = ForzaPacket.Gear(b, f);
            float spMph = ForzaPacket.SpeedMetersPerSecond(b, f) * 2.236936f;
            bool raceOn = ForzaPacket.IsRaceOn(b);

            var s = stats.GetValueOrDefault(g);
            s.n++; s.sumSpeed += spMph; if (raceOn) s.raceOn++; if (spMph > s.maxSpeed) s.maxSpeed = spMph;
            stats[g] = s;

            if (prevGear == 11 && g != 11)
                transitionsFrom11[g] = transitionsFrom11.GetValueOrDefault(g) + 1;
            prevGear = g;
        }

        Console.WriteLine($"Gear analysis for {Path.GetFileName(path)}:\n");
        Console.WriteLine($"  {"gear",4} {"count",8} {"avgMph",8} {"maxMph",8} {"raceOn%",8}");
        foreach (var kv in stats.OrderBy(k => k.Key))
        {
            var s = kv.Value;
            Console.WriteLine($"  {kv.Key,4} {s.n,8} {s.sumSpeed / s.n,8:F0} {s.maxSpeed,8:F0} {100.0 * s.raceOn / s.n,7:F0}%");
        }
        if (transitionsFrom11.Count > 0)
            Console.WriteLine($"\n  After gear 11, the next (different) gear was: " +
                string.Join(", ", transitionsFrom11.OrderByDescending(k => k.Value).Select(k => $"G{k.Key}({k.Value})")));
        Console.WriteLine("\n  Interpretation: very-low/zero avg speed + appears between gears => neutral. " +
            "High value at standstill while reversing => reverse.");
        return 0;
    }
}
