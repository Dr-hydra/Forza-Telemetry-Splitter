# Forza Telemetry Splitter

Send Forza Horizon's telemetry to **multiple tools at once**.

Forza Horizon 6's "Data Out" telemetry can only be sent to **one** IP:port. That means you
normally have to choose: feed [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu)
(auto-shifting) **or** an auto-tuner **or** a dashboard — never all three.

**Forza Telemetry Splitter** fixes that. It binds the single port Forza sends to, then re-broadcasts
every packet — **byte-for-byte, in real time** — to as many local tools as you want. Sub-millisecond
overhead, no data altered.

> Why you can't just point two apps at the same port: on Windows, a UDP unicast packet sent to
> `127.0.0.1` is delivered to only **one** socket (even with `SO_REUSEADDR`). A fan-out relay is the
> correct, lossless way to share the stream.

---

## Features (v0.1)

- 🔀 **Lossless fan-out** — splits FH6 "Car Dash" (324-byte) telemetry to any number of destinations, packets untouched.
- 🟢 **Top-right status overlay** — a tiny pill that shows **Connected / No data** at a glance while you drive. Toggle from the tray.
- 🧩 **Tool presets** — add destinations from a dropdown of known telemetry tools (VirtualTCU, co-driver, ai-tuner, fh6-tel, Tune It Yourself) or a custom IP:port.
- 🖥️ **Lives in the system tray** — runs quietly in the background like VirtualTCU.
- 🔒 **No Administrator required** — only does localhost UDP, so no UAC prompt.
- 📦 **Single `.exe`, nothing to install** — self-contained; no .NET runtime needed.

---

## Quick start

1. **Download** `ForzaTelemetrySplitter.exe` from the [Releases](../../releases) page and run it.
   - First launch shows a Windows SmartScreen warning (the app isn't code-signed yet).
     Click **More info → Run anyway**. *(Code signing is planned — see Roadmap.)*
2. The app starts in your **system tray** and begins splitting automatically.
3. **Point Forza at the splitter.** In FH6:
   `Settings → HUD and Gameplay → Data Out`
   - **Data Out:** ON
   - **IP Address:** `127.0.0.1`
   - **Port:** `44405`  ← the splitter's own port
   - **Packet Format:** Car Dash
4. **Leave your existing tools exactly as they are.** The splitter forwards to each tool's
   normal default port — you don't reconfigure anything:
   - **VirtualTCU** keeps its usual `5555` (it's enabled as a destination by default).
   - To add another tool: open the splitter (double-click the tray icon) → **Add** → pick it from
     the preset list (its default port is filled in) → done.
5. Drive. The overlay turns **green** and every tool receives the stream simultaneously.

> **Why a dedicated port?** Older guides told you to move VirtualTCU off 5555 so a splitter could
> steal it — which breaks the moment VirtualTCU launches first. Instead, this splitter listens on
> its **own** port (`44405`, used by no known Forza tool and outside Forza's reserved 5200–5300
> range) and forwards *to* your tools on the ports they already use. Nothing to reconfigure, no
> port fights.

### Default port map

| What | IP | Port |
|------|----|----|
| **Forza Data Out → Splitter (listen)** | 127.0.0.1 | **44405** |
| → VirtualTCU (its normal port, unchanged) | 127.0.0.1 | 5555 |
| → Your tuner (example, disabled by default) | 127.0.0.1 | 9999 |

You can change any of these in the app.

---

## Which tools does this help?

Only tools that **read Forza's live UDP telemetry**. Examples:

The splitter forwards to each tool on **its own normal port** — you don't change the tool.

| Tool | Its default port | Notes |
|------|--------------|-------|
| [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu) | 5555 | Auto-shifting. Unchanged — keep 5555. |
| [ForzaDash](https://github.com/himanshupapola/ForzaDash) | 1234 | Open-source FH6 telemetry dashboard. |
| [Forza-data-tools](https://github.com/richstokes/Forza-data-tools) | 9999 | Open-source CLI + browser dashboard. |
| [SIM Dashboard](https://www.stryder-it.de/simdashboard/) | 5685 | Phone/tablet dashboard — use the device's IP. |
| [SimHub](https://www.simhubdash.com/) | 20777 | Dashboard/effects suite. |
| [co-driver](https://github.com/Ojansen/co-driver) | 5300 | MIT dyno + tune workbench. ⚠ 5300 edges Forza's reserved 5200–5300 range. |
| [Tune It Yourself](https://www.tuneityourself.co.uk/) | over Wi-Fi | Live-telemetry auto-tuner (paid). Use the device's IP, not 127.0.0.1. |

> **Calculator tuners don't need this.** Tools like ForzaTune that you type car stats into never read
> telemetry — running the splitter does nothing for them.

---

## The status overlay

A small pill auto-positions in the **top-right** of your primary screen:

- 🟢 **Connected** — valid Forza packets are flowing right now.
- 🔴 **No data** — nothing arriving (you're in a menu, or Forza's Data Out isn't pointed here).

Toggle it from the tray menu (**Show overlay**). It never steals focus from the game.

> **Run Forza in Borderless / Windowed mode** for the overlay to show over the game. True
> fullscreen-exclusive mode can hide *any* overlay — that's a Windows/DirectX limitation, not specific
> to this app.

---

## Build from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (10.0+).

```sh
# Run locally
dotnet run --project src/ForzaTelemetrySplitter

# Produce the single self-contained .exe
dotnet publish src/ForzaTelemetrySplitter -c Release -r win-x64 -o publish
```

### Verify the engine (no game needed)

```sh
dotnet run --project tests/EngineTest -c Release
```

This sends fake 324-byte packets through the real `SplitterEngine` and confirms multiple
destinations receive byte-identical data, plus port-conflict handling.

---

## Project layout

```
src/ForzaTelemetrySplitter/
  Core/    SplitterEngine (fan-out), ForzaPacket, Destination
  Config/  AppConfig, ConfigStore (%APPDATA%\ForzaTelemetrySplitter\config.json), TunerPresets
  UI/      MainForm, DestinationDialog, OverlayForm, TrayContext
tests/EngineTest/   headless verification of the engine
tools/loopback-test.ps1   manual end-to-end test against a running app
```

---

## Roadmap (v0.2+)

Auto-start with Windows · auto-update · CSV logging · conditional forwarding (only when racing) ·
forwarding to phones/tablets on the LAN · FH5 / Forza Motorsport packet auto-detect (232/311/324) ·
live gear/RPM/speed mini-readout · global hotkey for the overlay · code signing.

## License

[MIT](LICENSE).

Not affiliated with or endorsed by Turn 10, Playground Games, or Microsoft.
"Forza" is a trademark of Microsoft.
