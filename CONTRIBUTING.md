# Contributing

Thanks for your interest in improving Forza Telemetry Splitter. It's a small, open-source tool, and
contributions are welcome.

## Ways to help

- Report a bug — see [docs/REPORTING-BUGS.md](docs/REPORTING-BUGS.md).
- Suggest a feature or a tool preset by opening an issue.
- Improve the docs.
- Send a pull request.

## Pull requests

1. Fork the repo and create a branch for your change.
2. Build and run the tests:
   ```sh
   dotnet build ForzaTelemetrySplitter.sln -c Release
   dotnet run --project tests/EngineTest -c Release
   ```
   See [docs/BUILDING.md](docs/BUILDING.md) for the full build, including the installer.
3. Keep the change focused, and match the style of the surrounding code.
4. If you touch the forwarding engine, make sure `tests/EngineTest` still passes; add a check if you're
   adding behavior.
5. Open the pull request with a short description of what changed and why.

## Adding a tool preset

Known telemetry tools live in `src/ForzaTelemetrySplitter/Config/TunerPresets.cs`. To add one, include
its name, its real default listen port, and a one-line note. Only tools that actually read Forza's UDP
"Data Out" stream belong there — calculator tuners do not.

## Scope

The goal is to do one job well: split Forza telemetry to multiple tools, reliably and without altering
the data. Changes that keep it small, dependable, and easy for non-technical users to run are the most
welcome.

## License

By contributing, you agree that your contributions are licensed under the project's
[MIT License](LICENSE).
