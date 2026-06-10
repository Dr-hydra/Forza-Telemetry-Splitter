using ForzaTelemetrySplitter.Core;

namespace ForzaTelemetrySplitter.Config;

/// <summary>
/// Persisted application settings. Serialized to JSON in %APPDATA%.
/// </summary>
public sealed class AppConfig
{
    /// <summary>IP the splitter binds to receive Forza's "Data Out" stream.</summary>
    public string ListenIp { get; set; } = "127.0.0.1";

    /// <summary>Port the splitter binds. Forza's Data Out must point here.</summary>
    public int ListenPort { get; set; } = 5555;

    /// <summary>Whether the top-right status overlay is visible.</summary>
    public bool ShowOverlay { get; set; } = true;

    /// <summary>Start splitting automatically when the app launches.</summary>
    public bool AutoStartSplitting { get; set; } = true;

    public List<Destination> Destinations { get; set; } = new();

    /// <summary>
    /// The configuration a brand-new user gets on first run: VirtualTCU enabled on 5556,
    /// plus a disabled placeholder they can rename to their tuner of choice.
    /// </summary>
    public static AppConfig CreateDefault() => new()
    {
        ListenIp = "127.0.0.1",
        ListenPort = 5555,
        ShowOverlay = true,
        AutoStartSplitting = true,
        Destinations = new List<Destination>
        {
            new() { Name = "VirtualTCU", Ip = "127.0.0.1", Port = 5556, Enabled = true },
            new() { Name = "My Tuner",   Ip = "127.0.0.1", Port = 5300, Enabled = false },
        },
    };
}
