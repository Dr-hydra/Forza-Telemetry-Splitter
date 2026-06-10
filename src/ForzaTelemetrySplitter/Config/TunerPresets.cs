namespace ForzaTelemetrySplitter.Config;

/// <summary>
/// A known telemetry-consuming tool the user might forward to. Only tools that actually
/// read Forza's UDP "Data Out" stream belong here — calculator tuners like ForzaTune do
/// not read telemetry and are intentionally excluded.
/// </summary>
public sealed record TunerPreset(string Name, int DefaultPort, string Note);

/// <summary>
/// Catalog used to populate the add/edit destination dropdown. Ports are sensible defaults;
/// users can override them, since any tool can be reconfigured to a different listen port.
/// </summary>
public static class TunerPresets
{
    public static readonly IReadOnlyList<TunerPreset> All = new[]
    {
        new TunerPreset("VirtualTCU", 5556,
            "Auto-shifting transmission controller. Default listen is 5555 — change it to 5556 so the splitter can own 5555."),
        new TunerPreset("co-driver", 5300,
            "Open-source (MIT) dyno + tune workbench + dashboard. Listens on 5300 for Forza."),
        new TunerPreset("ai-tuner", 5557,
            "AI race-engineer overlay with live tuning suggestions. Confirm/adjust its listen port in the app."),
        new TunerPreset("fh6-tel", 5558,
            "Telemetry dashboard with session recording/replay. Confirm/adjust its listen port in the app."),
        new TunerPreset("Tune It Yourself", 5559,
            "Live-telemetry auto-tuner (paid). Often receives over Wi-Fi to a phone/tablet — use that device's IP instead of 127.0.0.1."),
        new TunerPreset("Custom…", 5560,
            "Any other tool. Enter its name, IP and listen port manually."),
    };

    /// <summary>The entry used for fully manual destinations.</summary>
    public static TunerPreset Custom => All[^1];
}
