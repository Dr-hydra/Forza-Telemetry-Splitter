# Code signing

Signing the released binaries removes the "unknown publisher" line in Windows and lets the download
build SmartScreen trust over time. The build is already wired for it — signing turns on automatically
once a signing token is added as a repository secret. Until then, builds and releases work normally,
just unsigned.

The recommended path for an open-source project is **SignPath Foundation**, which provides free code
signing to qualifying OSS projects.

## Why SignPath (and the one caveat)

- Free for open source, a real trusted-CA signature, and it works in GitHub Actions (no USB token).
- It does not give *instant* SmartScreen trust — no certificate does anymore (Microsoft changed this in
  2024). Signing fixes "unknown publisher" immediately; full SmartScreen trust then accrues as people
  download the signed releases.
- Fallback if SignPath doesn't fit: **Azure Artifact Signing** (~$10/month) also works in CI and needs
  no USB token, but requires a paid Azure subscription and identity verification.

## Eligibility checklist (this project)

SignPath Foundation requires, and this project meets:

- OSI-approved license, no commercial dual-licensing — MIT.
- No closed-source or proprietary components.
- Actively maintained and already released — yes (v0.1.0 is public).
- Built from source in a verifiable way — yes, via the GitHub Actions workflow.
- Functionality documented on the download page — yes (README + Releases).

## Apply

1. Read the program terms and apply at the SignPath Foundation open-source page:
   https://signpath.org/ (Open Source Software).
2. In the application, point them at this repository and its GitHub Actions build.
3. Approval is manual and can take some time; SignPath signs each release after a manual review.

## Once approved

SignPath will give you an organization ID and an API token, and you'll define a project, a signing
policy, and two artifact configurations.

1. In the repo: Settings > Secrets and variables > Actions, add:
   - `SIGNPATH_API_TOKEN` — the API token.
   - `SIGNPATH_ORG_ID` — your SignPath organization ID.
2. In SignPath, create/confirm these slugs to match the workflow
   (`.github/workflows/build.yml`):
   - project: `forza-telemetry-splitter`
   - signing policy: `release-signing`
   - artifact configurations: `app-exe` (the published exe) and `installer-exe` (the Inno installer).
3. That's it. The next time CI runs, the gated steps detect the token and:
   - sign `ForzaTelemetrySplitter.exe`,
   - build the installer with `/DSign` so the installer and uninstaller are signed,
   - sign `ForzaTelemetrySplitterInstaller.exe`.

No certificate or token is ever committed to the repo — only the wiring that consumes the secrets.

## Local signing (optional)

If you have a signing tool configured locally, you can sign the installer directly:

```sh
ISCC.exe /DSign /Ssigntool="<signtool> sign /fd sha256 /tr <timestamp-url> /td sha256 $f" installer\ForzaTelemetrySplitter.iss
```

Without `/DSign`, the installer compiles unsigned (the normal dev build).
