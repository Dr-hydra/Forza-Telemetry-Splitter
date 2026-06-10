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
}
