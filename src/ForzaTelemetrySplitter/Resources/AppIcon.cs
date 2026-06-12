using System.Reflection;

namespace ForzaTelemetrySplitter.Resources;

/// <summary>
/// Loads the bundled app logo (embedded as resources) for the tray icon, window icons, and the
/// welcome image. Falls back to the system application icon if the resource can't be loaded, so the
/// app never fails to start over a missing asset.
/// </summary>
public static class AppIcon
{
    private const string IcoResource = "ForzaTelemetrySplitter.logo.ico";
    private const string PngResource = "ForzaTelemetrySplitter.logo.png";

    /// <summary>The app icon (.ico) for the tray and window title bars. Caller owns disposal where it
    /// keeps the instance for the app lifetime.</summary>
    public static Icon Load()
    {
        try
        {
            using var s = typeof(AppIcon).Assembly.GetManifestResourceStream(IcoResource);
            if (s is not null) return new Icon(s);
        }
        catch { /* fall through */ }
        return SystemIcons.Application;
    }

    /// <summary>The transparent logo image (.png) for the welcome window.</summary>
    public static Image? LoadImage()
    {
        try
        {
            using var s = typeof(AppIcon).Assembly.GetManifestResourceStream(PngResource);
            if (s is not null) return Image.FromStream(s);
        }
        catch { /* ignore */ }
        return null;
    }
}
