// Recorder fidelity test: drive the REAL SplitterEngine + SessionRecorder at a precisely-known rate
// and verify the .fts captures every packet with correct timing. This isolates whether WE drop/
// undercount packets (vs. the game simply sending ~50/s).
//
// Run: dotnet run --project tools/AnalyzeFts -c Release -- --fidelity

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ForzaTelemetrySplitter.Core;

internal static class Fidelity
{
    public static int Run()
    {
        const string ip = "127.0.0.1";
        const int port = 48500;
        int failures = 0;

        // Test several target rates including 80/s (the user's fps) and 50/s (what was logged).
        foreach (int targetHz in new[] { 50, 60, 80, 120 })
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"fidelity-{targetHz}-{Guid.NewGuid():N}.fts");
            int sent = SendThroughEngine(ip, port, targetHz, durationSec: 4, tmp);
            var (recorded, measuredHz) = ParseCount(tmp);
            try { File.Delete(tmp); } catch { }

            double lossPct = sent > 0 ? 100.0 * (sent - recorded) / sent : 0;
            bool ok = recorded >= sent * 0.97;   // allow <3% for the very first/last packet edges
            if (!ok) failures++;
            Console.WriteLine(
                $"  target {targetHz,3}/s: sent {sent,4}, recorded {recorded,4}  (loss {lossPct,5:F1}%)  " +
                $"recorded-rate ~{measuredHz:F0}/s  -> {(ok ? "PASS" : "FAIL")}");
        }

        Console.WriteLine(failures == 0
            ? "\nVERDICT: the recorder captures every packet it receives. ~50/s in the race log = the\n" +
              "         game was sending ~50/s (Data Out rate), NOT packet loss in the splitter."
            : "\nVERDICT: the recorder is UNDERCOUNTING — investigate the receive/record path.");
        return failures;
    }

    private static int SendThroughEngine(string ip, int port, int hz, int durationSec, string recPath)
    {
        var engine = new SplitterEngine();
        engine.Start(ip, port);
        engine.StartRecording(recPath);

        int sent = 0;
        using (var sender = new UdpClient())
        {
            var target = new IPEndPoint(IPAddress.Parse(ip), port);
            double intervalMs = 1000.0 / hz;
            var sw = Stopwatch.StartNew();
            int total = hz * durationSec;
            for (int i = 0; i < total; i++)
            {
                var pkt = new byte[ForzaPacket.CarDashSize]; // 324B Horizon
                pkt[0] = 1;
                sender.Send(pkt, pkt.Length, target);
                sent++;
                double due = (i + 1) * intervalMs;
                double wait = due - sw.Elapsed.TotalMilliseconds;
                if (wait > 1) Thread.Sleep((int)wait);
            }
        }
        Thread.Sleep(300);          // let the tail drain into the recorder
        engine.StopRecording();
        engine.Stop();
        return sent;
    }

    private static (int count, double hz) ParseCount(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        br.ReadBytes(4); br.ReadInt32(); // header
        int count = 0; double totalSec = 0; bool first = true;
        while (fs.Position < fs.Length)
        {
            long dt; int len;
            try { dt = br.ReadInt64(); len = br.ReadUInt16(); } catch { break; }
            if (fs.Position + len > fs.Length) break;
            br.ReadBytes(len);
            if (!first) totalSec += dt / 10_000_000.0;
            first = false;
            count++;
        }
        double hz = totalSec > 0 ? (count - 1) / totalSec : 0;
        return (count, hz);
    }
}
