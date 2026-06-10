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
   - **Port:** `5555`
   - **Packet Format:** Car Dash
4. **Move your other tools off port 5555** so the splitter can own it, and add them as destinations:
   - **VirtualTCU:** change its listen port from `5555` to `5556` (it's already a destination by default).
   - Open the splitter (double-click the tray icon) → **Add** → pick your tuner from the preset list → set the port it listens on.
5. Drive. The overlay turns **green** and each tool receives the stream simultaneously.

### Default port map

| What | IP | Port |
|------|----|----|
| **Forza Data Out → Splitter (listen)** | 127.0.0.1 | **5555** |
| VirtualTCU (set its listen port to this) | 127.0.0.1 | 5556 |
| Your tuner (example, disabled by default) | 127.0.0.1 | 5300 |

You can change any of these in the app.

---

## Which tools does this help?

Only tools that **read Forza's live UDP telemetry**. Examples:

| Tool | Default port | Notes |
|------|--------------|-------|
| [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu) | 5555 → **5556** | Auto-shifting. Change its listen port so the splitter can own 5555. |
| [co-driver](https://github.com/Ojansen/co-driver) | 5300 | Open-source (MIT) dyno + tune workbench + dashboard. |
| [ai-tuner](https://github.com/diogojesusdev/ai-tuner) | (set in app) | AI race-engineer overlay with tuning suggestions. |
| [fh6-tel](https://github.com/TheBanHammer/fh6-tel) | (set in app) | Telemetry dashboard + session recording. |
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
