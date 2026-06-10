using System.Buffers.Binary;

namespace ForzaTelemetrySplitter.Core;

/// <summary>
/// Knowledge about the Forza Horizon "Data Out" wire format.
///
/// FH6 uses the "Car Dash" packet, which is byte-for-byte identical to FH5: a fixed
/// 324 bytes. The splitter never parses the body — it forwards bytes untouched — but it
/// reads a single field (IsRaceOn at offset 0) purely to drive the connection indicator.
/// </summary>
public static class ForzaPacket
{
    /// <summary>FH6 / FH5 "Car Dash" packet size in bytes.</summary>
    public const int CarDashSize = 324;

    /// <summary>
    /// True when the datagram is a plausible Forza Car Dash packet.
    /// We accept exactly 324 bytes; anything else is not the format we expect.
    /// </summary>
    public static bool IsCarDash(int length) => length == CarDashSize;

    /// <summary>
    /// IsRaceOn lives at byte offset 0 (a 32-bit int, but the low byte is sufficient to
    /// distinguish 1 = actively driving from 0 = menu/paused). Forza actually stops sending
    /// packets in menus, so this mostly reports ON while data flows.
    /// </summary>
    public static bool IsRaceOn(ReadOnlySpan<byte> packet)
        => packet.Length >= CarDashSize && packet[0] != 0;

    // Horizon "Car Dash" field offsets. The Horizon layout has a 12-byte gap after the Sled
    // section (offset 232), which pushes the dash fields later than the Motorsport layout.
    // These are verified by a round-trip test in tests/EngineTest before being trusted.
    public const int SpeedOffset = 256; // float32, metres per second
    public const int GearOffset = 319;  // uint8

    /// <summary>Vehicle speed in metres per second (the raw unit Forza sends, regardless of the
    /// game's display units). Returns 0 for a too-short packet.</summary>
    public static float SpeedMetersPerSecond(ReadOnlySpan<byte> packet)
        => packet.Length >= CarDashSize
            ? BinaryPrimitives.ReadSingleLittleEndian(packet.Slice(SpeedOffset, 4))
            : 0f;

    /// <summary>Current gear as Forza reports it (0 = neutral/no gear). Returns 0 for a too-short
    /// packet.</summary>
    public static int Gear(ReadOnlySpan<byte> packet)
        => packet.Length >= CarDashSize ? packet[GearOffset] : 0;
}
