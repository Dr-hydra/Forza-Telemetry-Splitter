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
- 🧩 **Tool presets** — add destinations from a dropdown of known telemetry tools (VirtualTCU, ForzaDash, SimHub, SIM Dashboard, co-driver…) or a custom IP:port.
- 🖥️ **Lives in the system tray** — runs quietly in the background like VirtualTCU.
- 🔒 **No Administrator required** — only does localhost UDP, so no UAC prompt.
- 📦 **One small installer** (or a portable `.exe`) — self-contained; no .NET runtime to install.

---

## Install

**Recommended — the installer:**

1. Download **`ForzaTelemetrySplitterSetup.exe`** from the [Releases](../../releases) page.
2. *(Recommended)* Right-click it → **Properties** → tick **Unblock** (bottom of the General tab) → **OK**.
   This avoids the blue "Windows protected your PC" screen (see [SmartScreen](#about-the-windows-smartscreen-warning) below).
3. Run it. The installer is **per-user — no Administrator prompt**. It offers a desktop shortcut and an
   optional **"Start automatically when Windows starts"** checkbox.
4. It launches into your **system tray** when finished.

**Prefer no install?** Download the portable **`ForzaTelemetrySplitter.exe`** instead and just run it —
it's the same app, you just manage the file and shortcuts yourself.

> **Can't find the tray icon?** Windows hides new tray icons by default. Click the small **`^`
> chevron** at the bottom-right of the taskbar — it'll be in there. Drag it onto the taskbar to keep
> it visible.

---

## First-time setup (do this once)

The app starts splitting automatically; you just have to point Forza at it.

**1. Open the app** (double-click the tray icon). You'll see it's listening on port **`44405`** and
already set to forward to **VirtualTCU** on its normal `5555`.

**2. Point Forza at the splitter.** In FH6:
`Settings → HUD and Gameplay → Data Out`
- **Data Out:** ON
- **IP Address:** `127.0.0.1`
- **Port:** **`44405`**  ← the splitter's own port
- **Packet Format:** Car Dash

**3. Leave your existing tools as they are.** The splitter forwards to each tool on the port it
already uses — **don't reconfigure them**:
- **VirtualTCU** stays on `5555` (already enabled as a destination).
- **Add another tool:** in the app, click **Add** → pick it from the preset list (its default port is
  filled in for you) → **OK**. Or choose **Custom…** for anything else.

**4. Drive.** When telemetry is flowing, the **top-right pill turns green ("Connected")** and every
enabled tool receives the stream at the same time. 🎉

> **Why a dedicated port (44405)?** Older guides told you to move VirtualTCU off `5555` so a splitter
> could steal it — which breaks the moment VirtualTCU launches first and grabs `5555`. Instead, this
> splitter listens on its **own** port (`44405` — used by no known Forza tool and outside Forza's
> reserved `5200–5300` range) and forwards *to* your tools on the ports they already use. Nothing to
> reconfigure, no port fights.

### Default port map

| What | IP | Port |
|------|----|----|
| **Forza Data Out → Splitter (listen)** | 127.0.0.1 | **44405** |
| → VirtualTCU (its normal port, unchanged) | 127.0.0.1 | 5555 |
| → Your tuner (example, disabled by default) | 127.0.0.1 | 9999 |

You can change any of these in the app. If you ever see a **"port already in use"** message, another
app is on the splitter's listen port — just change the splitter's port in the app and set Forza's Data
Out port to match.

---

## About the Windows SmartScreen warning

This app is open source and safe, but it's **new and not yet code-signed**, so Windows doesn't
recognize it yet and may show **"Windows protected your PC."** This is normal for new indie apps —
trust builds as more people download it. To run it:

- **Best:** right-click the downloaded file → **Properties** → tick **Unblock** → **OK**, *then* run it.
- **Or:** on the blue screen, click **More info** → **Run anyway**.
- **Verify integrity (optional):** compare `Get-FileHash .\ForzaTelemetrySplitterSetup.exe` to the
  SHA-256 in the release notes.

*(Code signing is on the roadmap — it removes the "unknown publisher" line; full SmartScreen trust
then builds with downloads.)*

---

## Updating

There's **no background auto-update** (this is a small, stable tool). To update:

1. **Tray menu → "Check for updates…"** opens the [Releases](../../releases) page.
2. If there's a newer version, download the new **`ForzaTelemetrySplitterSetup.exe`** and run it.
3. It **upgrades in place** — same folder, same settings, no duplicate install. (It replaces the old
   version automatically; your destinations and preferences in `%APPDATA%` are preserved.)

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

# Produce the single self-contained .exe (lands in publish/)
dotnet publish src/ForzaTelemetrySplitter -c Release -r win-x64 -o publish
```

Build the installer (requires [Inno Setup 6](https://jrsoftware.org/isdl.php)):

```sh
# After publishing, compile publish/ForzaTelemetrySplitterSetup.exe
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\ForzaTelemetrySplitter.iss
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
installer/ForzaTelemetrySplitter.iss   Inno Setup script (builds the installer)
```

---

## Roadmap (v0.2+)

CSV logging · conditional forwarding (only when racing) · forwarding to phones/tablets on the LAN ·
FH5 / Forza Motorsport packet auto-detect (232/311/324) · live gear/RPM/speed mini-readout · global
hotkey for the overlay · code signing (free OSS signing via SignPath, or Azure Artifact Signing) ·
optional winget / Microsoft Store listing.

*(The installer already covers "start with Windows" as an opt-in. Auto-update was intentionally left
out — it's a small, stable utility; use **Check for updates** instead.)*

## License

[MIT](LICENSE).

Not affiliated with or endorsed by Turn 10, Playground Games, or Microsoft.
"Forza" is a trademark of Microsoft.
