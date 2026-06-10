# Reporting a bug

Found something broken? Please open an issue:

[Open a new issue](https://github.com/jakemismas/Forza-Telemetry-Splitter/issues/new/choose)

Before filing, a quick check of [existing issues](https://github.com/jakemismas/Forza-Telemetry-Splitter/issues)
saves duplicates.

## What to include

The more of this you can give, the faster it gets fixed:

- What you expected to happen, and what actually happened.
- Steps to reproduce it.
- Your Windows version (10 or 11) and the app version (shown in the app's title bar, or in Add/Remove
  Programs).
- Your setup: the splitter's listen port, the destinations you configured, and which other tools were
  running (VirtualTCU, a tuner, a dashboard).
- Whether the top-right overlay was green or red at the time.
- Any error message the app showed — copy the exact text.

## Telemetry isn't reaching a tool? Try this first

- Confirm your Forza game's Data Out is set to the splitter's port (44405 by default), IP 127.0.0.1,
  Packet Format Car Dash (Horizon) or Dash (Motorsport). The app shows which game it detects.
- Confirm the tool is listed and enabled as a destination, on the port that tool actually listens on.
- The overlay should be green while you drive. Red means Forza isn't sending to the splitter.
- Avoid Forza's reserved 5200–5300 port range for destinations.

If those don't explain it, include what you saw in the issue.
