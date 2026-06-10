using ForzaTelemetrySplitter.Core;

namespace ForzaTelemetrySplitter.Config;

/// <summary>
/// Persisted application settings. Serialized to JSON in %APPDATA%.
/// </summary>
public sealed class AppConfig
{
    /// <summary>IP the splitter binds to receive Forza's "Data Out" stream.</summary>
    public string ListenIp { get; set; } = "127.0.0.1";

    /// <summary>
    /// Port the splitter binds. Forza's Data Out must point here. We use a dedicated port
    /// (44405) that no known Forza tool uses and that sits outside Forza's reserved 5200–5300
    /// range — so the splitter never fights an existing tool for a port, and users don't have
    /// to reconfigure tools they already have working. The splitter then forwards to each
    /// tool's existing default port (e.g. VirtualTCU on 5555, untouched).
    /// </summary>
    public int ListenPort { get; set; } = 44405;

    /// <summary>Whether the top-right status overlay is visible.</summary>
    public bool ShowOverlay { get; set; } = true;

    /// <summary>Start splitting automatically when the app launches.</summary>
    public bool AutoStartSplitting { get; set; } = true;

    /// <summary>
    /// True once the user has seen the first-run welcome window. Defaults false so the welcome
    /// (which walks through Forza's Data Out settings) shows on the very first launch only.
    /// </summary>
    public bool FirstRunComplete { get; set; }

    /// <summary>Display unit for the live speed readout. Defaults from the Windows region.</summary>
    public SpeedUnit SpeedUnit { get; set; } = SpeedUnitExtensions.FromRegion();

    public List<Destination> Destinations { get; set; } = new();

    /// <summary>
    /// The configuration a brand-new user gets on first run: VirtualTCU enabled on 5556,
    /// plus a disabled placeholder they can rename to their tuner of choice.
    /// </summary>
    public static AppConfig CreateDefault() => new()
    {
        ListenIp = "127.0.0.1",
        ListenPort = 44405,
        ShowOverlay = true,
        AutoStartSplitting = true,
        Destinations = new List<Destination>
        {
            // Forwards to each tool's EXISTING default listen port — nothing to reconfigure.
            // VirtualTCU keeps its normal 5555. The second slot is a disabled example the
            // user can enable/rename for their tuner.
            new() { Name = "VirtualTCU", Ip = "127.0.0.1", Port = 5555, Enabled = true },
            new() { Name = "My Tuner",   Ip = "127.0.0.1", Port = 9999, Enabled = false },
        },
    };
}
