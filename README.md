# Forza Telemetry Splitter

Send Forza Horizon's telemetry to several tools at once.

Forza Horizon 6's "Data Out" telemetry can only be sent to one IP and port. That forces a choice:
feed [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu) for auto-shifting, or an auto-tuner,
or a dashboard — but not all of them together.

Forza Telemetry Splitter sits in between. It receives Forza's telemetry on its own port and re-sends
every packet, unchanged, to as many local tools as you like. The overhead is sub-millisecond and the
data is not altered, so each tool behaves exactly as if it were talking to Forza directly.

Not affiliated with or endorsed by Turn 10, Playground Games, or Microsoft. "Forza" is a trademark of
Microsoft.

## Features

| Feature | Description |
|---------|-------------|
| Fan-out | Splits FH6 "Car Dash" (324-byte) telemetry to any number of destinations, packets untouched. |
| Status overlay | A small pill in the top-right shows "Connected" or "No data" while you drive, with a live gear and speed readout. Toggle it from the tray. |
| Speed units | Shows mph or kph, defaulted from your Windows region and switchable in the app. |
| Tool presets | Add destinations from a list of known telemetry tools (VirtualTCU, ForzaDash, SimHub, SIM Dashboard, co-driver) or a custom IP and port. |
| Tray app | Runs quietly in the system tray, like VirtualTCU. |
| No Administrator | Only does localhost UDP, so there's no UAC prompt. |
| One installer | A small per-user installer, or a portable single .exe. No .NET runtime to install. |

## Install

Recommended — the installer:

1. Download `ForzaTelemetrySplitterSetup.exe` from the [Releases](../../releases) page.
2. Right-click it, choose Properties, tick Unblock at the bottom of the General tab, then OK. This
   avoids the "Windows protected your PC" screen. See [docs/SMARTSCREEN.md](docs/SMARTSCREEN.md) if you
   still see it.
3. Run it. The installer is per-user, so there's no Administrator prompt. It offers a desktop shortcut
   and an optional "Start automatically when Windows starts" checkbox.
4. It launches into the system tray when finished.

Prefer no install? Download the portable `ForzaTelemetrySplitter.exe` instead and run it directly.

If you can't find the tray icon, Windows hides new ones by default. Click the small chevron (^) at the
bottom-right of the taskbar, then drag the icon onto the taskbar to keep it visible.

## In-game setup

The app starts splitting automatically; you only have to point Forza at it.

1. Open the app from the tray. It listens on port 44405 and is already set to forward to VirtualTCU on
   its normal port 5555.
2. In FH6, go to Settings, then HUD and Gameplay, then Data Out:
   - Data Out: ON
   - IP Address: 127.0.0.1
   - Port: 44405
   - Packet Format: Car Dash
3. Leave your other tools as they are. The splitter forwards to each tool on the port it already uses,
   so there's nothing to reconfigure. To add another tool, click Add in the app, pick it from the
   preset list, and click OK.
4. Drive. When telemetry is flowing, the top-right pill turns green and every enabled tool receives the
   stream at the same time.

The splitter uses its own port (44405) so it never has to take a port another tool already owns. That
avoids the conflict you get if two apps try to listen on 5555. If you ever see a "port already in use"
message, change the splitter's port in the app and set Forza's Data Out port to match.

### Default ports

| What | IP | Port |
|------|----|----|
| Forza Data Out, into the splitter | 127.0.0.1 | 44405 |
| Forwarded to VirtualTCU (unchanged) | 127.0.0.1 | 5555 |
| Forwarded to your tuner (example, off by default) | 127.0.0.1 | 9999 |

## Supported tools

Any tool that reads Forza's live UDP telemetry. The splitter forwards to each on its own normal port,
so you don't change the tool.

| Tool | Its default port | Notes |
|------|------------------|-------|
| [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu) | 5555 | Auto-shifting. Unchanged — keep 5555. |
| [ForzaDash](https://github.com/himanshupapola/ForzaDash) | 1234 | Open-source FH6 telemetry dashboard. |
| [Forza-data-tools](https://github.com/richstokes/Forza-data-tools) | 9999 | Open-source CLI and browser dashboard. |
| [SIM Dashboard](https://www.stryder-it.de/simdashboard/) | 5685 | Phone or tablet dashboard. Use the device's IP. |
| [SimHub](https://www.simhubdash.com/) | 20777 | Dashboard and effects suite. |
| [co-driver](https://github.com/Ojansen/co-driver) | 5300 | MIT dyno and tune workbench. 5300 edges Forza's reserved 5200–5300 range. |
| [Tune It Yourself](https://www.tuneityourself.co.uk/) | over Wi-Fi | Live-telemetry auto-tuner (paid). Use the device's IP, not 127.0.0.1. |

Calculator tuners such as ForzaTune do not read telemetry, so the splitter does nothing for them.

## The overlay

A small pill auto-positions in the top-right of the primary screen. Green means valid Forza packets are
arriving and shows your current gear and speed; red means none are (you're in a menu, or Forza's Data
Out isn't pointed at the splitter). Speed shows in mph or kph (set in the app, defaulted from your
Windows region). Toggle the overlay from the tray menu. It never steals focus from the game.

Run Forza in Borderless or Windowed mode for the overlay to show over the game. True fullscreen can hide
any overlay, which is a Windows limitation rather than something specific to this app.

## Updating

There's no background auto-update. Use Check for updates in the tray menu to open the Releases page. If
there's a newer version, download the new installer and run it — it upgrades in place, keeps a single
entry in Add/Remove Programs, and preserves your settings.

## Settings file

Settings live in `%APPDATA%\ForzaTelemetrySplitter\config.json` (listen port, destinations, overlay
on/off). Deleting it resets the app to defaults on next launch.

## More

- [Building from source](docs/BUILDING.md)
- [Windows SmartScreen warning](docs/SMARTSCREEN.md)
- [Code signing](docs/CODE-SIGNING.md)
- [Reporting a bug](docs/REPORTING-BUGS.md)
- [Contributing](CONTRIBUTING.md)
- [License (MIT)](LICENSE)

## Tested on

Windows 10 and Windows 11, with Forza Horizon 6 ("Car Dash" 324-byte packets).
