using ForzaTelemetrySplitter.Resources;

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
    // Ports below are each tool's OWN existing default listen port — the splitter forwards to
    // them as-is, so users don't reconfigure tools they already have working. (The splitter
    // itself listens on its own dedicated port; see AppConfig.ListenPort.)
    public static IReadOnlyList<TunerPreset> All => new[]
    {
        new TunerPreset("VirtualTCU", 5555, Strings.Tuner_VirtualTcuNote),
        new TunerPreset("ForzaDash", 1234, Strings.Tuner_ForzaDashNote),
        new TunerPreset("Forza-data-tools", 9999, Strings.Tuner_ForzaDataToolsNote),
        new TunerPreset("SIM Dashboard", 5685, Strings.Tuner_SimDashboardNote),
        new TunerPreset("SimHub", 20777, Strings.Tuner_SimHubNote),
        new TunerPreset("co-driver", 5300, Strings.Tuner_CoDriverNote),
        new TunerPreset("Tune It Yourself", 5685, Strings.Tuner_TuneItYourselfNote),
        new TunerPreset(Strings.Tuner_CustomName, 9000, Strings.Tuner_CustomNote),
    };

    /// <summary>The entry used for fully manual destinations.</summary>
    public static TunerPreset Custom => All[^1];
}
