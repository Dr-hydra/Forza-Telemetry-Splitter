using System.Buffers.Binary;

namespace ForzaTelemetrySplitter.Core;

/// <summary>
/// The Forza game/packet format, identified by datagram length. Every Forza title emits Data Out
/// as plain UDP; the layout (and therefore field offsets) differs by game, but the size is a
/// reliable discriminator.
/// </summary>
public enum ForzaFormat
{
    Unknown,
    Sled,            // 232 bytes — motion-platform format, no dashboard fields
    MotorsportDash,  // 311 / 331 bytes — Forza Motorsport 7 and FM 2023
    HorizonCarDash,  // 324 (or 323) bytes — Forza Horizon 4 / 5 / 6
}

/// <summary>
/// Knowledge about the Forza "Data Out" wire format across games.
///
/// The splitter forwards raw bytes and never needs to parse to relay. Parsing here is only for the
/// connection indicator and the gear/speed readout. The one field common to every format is
/// IsRaceOn at offset 0; the dash fields (Speed, Gear) sit at different offsets in the Horizon vs
/// Motorsport layouts because Horizon inserts a 12-byte gap after the Sled section.
/// </summary>
public static class ForzaPacket
{
    // Known datagram sizes.
    public const int SledSize = 232;
    public const int MotorsportDashSize = 311;
    public const int MotorsportExtrasSize = 331;
    public const int CarDashSize = 324;       // Horizon (kept name for existing callers)
    public const int CarDashAltSize = 323;    // some FH4 builds

    // Dash-field offsets per layout. Motorsport and Horizon differ by the 12-byte Horizon gap.
    private const int MotorsportSpeedOffset = 244;
    private const int MotorsportGearOffset = 307;
    private const int HorizonSpeedOffset = 256;
    private const int HorizonGearOffset = 319;

    /// <summary>Identify the format from the datagram length.</summary>
    public static ForzaFormat Detect(int length) => length switch
    {
        SledSize => ForzaFormat.Sled,
        MotorsportDashSize or MotorsportExtrasSize => ForzaFormat.MotorsportDash,
        CarDashSize or CarDashAltSize => ForzaFormat.HorizonCarDash,
        _ => ForzaFormat.Unknown,
    };

    /// <summary>True when the datagram length matches any recognized Forza format.</summary>
    public static bool IsValid(int length) => Detect(length) != ForzaFormat.Unknown;

    /// <summary>A friendly game name for the detected format.</summary>
    public static string GameName(ForzaFormat format) => format switch
    {
        ForzaFormat.MotorsportDash => "Forza Motorsport",
        ForzaFormat.HorizonCarDash => "Forza Horizon",
        ForzaFormat.Sled => "Forza (sled format)",
        _ => "—",
    };

    /// <summary>
    /// IsRaceOn lives at byte offset 0 in every format (1 = actively driving, 0 = menu/paused).
    /// Forza stops sending packets in menus, so this mostly reports ON while data flows.
    /// </summary>
    public static bool IsRaceOn(ReadOnlySpan<byte> packet)
        => IsValid(packet.Length) && packet[0] != 0;

    /// <summary>
    /// Vehicle speed in metres per second (the raw unit Forza sends, regardless of display units),
    /// using the offset for the given format. Returns 0 for formats without dash data (Sled/Unknown)
    /// or a too-short packet.
    /// </summary>
    public static float SpeedMetersPerSecond(ReadOnlySpan<byte> packet, ForzaFormat format)
    {
        int offset = format switch
        {
            ForzaFormat.HorizonCarDash => HorizonSpeedOffset,
            ForzaFormat.MotorsportDash => MotorsportSpeedOffset,
            _ => -1,
        };
        if (offset < 0 || packet.Length < offset + 4) return 0f;
        return BinaryPrimitives.ReadSingleLittleEndian(packet.Slice(offset, 4));
    }

    /// <summary>Current gear as Forza reports it (0 = neutral/no gear), using the format's offset.
    /// Returns 0 for formats without dash data or a too-short packet.</summary>
    public static int Gear(ReadOnlySpan<byte> packet, ForzaFormat format)
    {
        int offset = format switch
        {
            ForzaFormat.HorizonCarDash => HorizonGearOffset,
            ForzaFormat.MotorsportDash => MotorsportGearOffset,
            _ => -1,
        };
        if (offset < 0 || packet.Length <= offset) return 0;
        return packet[offset];
    }
}
