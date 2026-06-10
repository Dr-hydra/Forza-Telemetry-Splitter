# Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) 10.0 or newer. The installer step also
needs [Inno Setup 6](https://jrsoftware.org/isdl.php).

## Run locally

```sh
dotnet run --project src/ForzaTelemetrySplitter
```

## Produce the single self-contained .exe

```sh
dotnet publish src/ForzaTelemetrySplitter -c Release -r win-x64 -o publish
```

The result is `publish/ForzaTelemetrySplitter.exe` — a self-contained build, so users need no .NET
runtime.

## Build the installer

After publishing:

```sh
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\ForzaTelemetrySplitter.iss
```

This produces `publish/ForzaTelemetrySplitterSetup.exe`.

## Verify the engine (no game needed)

```sh
dotnet run --project tests/EngineTest -c Release
```

This sends fake 324-byte packets through the real `SplitterEngine` and confirms multiple destinations
receive byte-identical data, and that port conflicts are reported clearly. There is also
`tools/loopback-test.ps1`, which drives a running app end to end.

## Project layout

```
src/ForzaTelemetrySplitter/
  Core/    SplitterEngine (fan-out), ForzaPacket, Destination
  Config/  AppConfig, ConfigStore (%APPDATA%\ForzaTelemetrySplitter\config.json), TunerPresets
  UI/      MainForm, DestinationDialog, OverlayForm, TrayContext
tests/EngineTest/                       headless verification of the engine
tools/loopback-test.ps1                 manual end-to-end test against a running app
installer/ForzaTelemetrySplitter.iss    Inno Setup script (builds the installer)
```
