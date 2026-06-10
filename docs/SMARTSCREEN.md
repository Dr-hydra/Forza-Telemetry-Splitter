# The Windows SmartScreen warning

This app is open source and safe, but it's new and not yet code-signed, so Windows doesn't recognize it
and may show "Windows protected your PC." This is normal for new independent apps — the warning fades as
more people download it and Windows builds a reputation for the file.

## Run it anyway

Any of these work. The first is cleanest because it stops the warning from appearing at all:

1. Right-click the downloaded file, choose Properties, tick Unblock at the bottom of the General tab,
   then OK. Now run it.
2. Or, on the blue screen, click More info, then Run anyway.
3. Optional: verify the download first. Compare the output of
   `Get-FileHash .\ForzaTelemetrySplitterSetup.exe` with the SHA-256 listed in the release notes.

## Why it happens

Windows attaches a "downloaded from the internet" mark to files from a browser. SmartScreen checks that
file against its reputation database; a brand-new, unsigned file has no reputation yet, so it warns.
Unblocking removes the mark, which is why the warning then disappears.

[Code signing](CODE-SIGNING.md) is planned, which will remove the "unknown publisher" line
immediately. Full SmartScreen trust still builds with download volume even after signing.
