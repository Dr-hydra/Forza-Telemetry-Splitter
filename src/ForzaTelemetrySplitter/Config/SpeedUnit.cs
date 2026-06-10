using System.Globalization;

namespace ForzaTelemetrySplitter.Config;

/// <summary>
/// Display unit for speed. Forza always sends metres per second; this only affects how the
/// readout is shown. The game's stream contains no field for the player's preference, so we
/// can't auto-detect it — we default from the Windows region and let the user override.
/// </summary>
public enum SpeedUnit
{
    Mph,
    Kph,
}

public static class SpeedUnitExtensions
{
    /// <summary>Best-guess default from the Windows region (non-metric regions → Mph).</summary>
    public static SpeedUnit FromRegion()
    {
        try
        {
            return RegionInfo.CurrentRegion.IsMetric ? SpeedUnit.Kph : SpeedUnit.Mph;
        }
        catch
        {
            return SpeedUnit.Mph; // sensible fallback if region can't be determined
        }
    }

    /// <summary>Format a metres-per-second value as a rounded "112 mph" / "180 kph" string.</summary>
    public static string FormatSpeed(float metersPerSecond, SpeedUnit unit)
    {
        double value = unit == SpeedUnit.Kph
            ? metersPerSecond * 3.6
            : metersPerSecond * 2.236936;
        string suffix = unit == SpeedUnit.Kph ? "kph" : "mph";
        return $"{Math.Max(0, (int)Math.Round(value))} {suffix}";
    }

    /// <summary>Format a gear value: 0 shows as "N", otherwise the number.</summary>
    public static string FormatGear(int gear) => gear <= 0 ? "N" : gear.ToString();
}
