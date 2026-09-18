# Comet v0.1.0 Release Candidate validation

Validated on Windows 11 before the v0.1.0 tag.

## CI / packaging

- Restore: passed.
- Release build: passed.
- Smoke tests: passed.
- Self-contained win-x64 publish: passed.
- Inno Setup installer compilation: passed.
- Release-candidate ZIP, Setup.exe, and SHA256SUMS generation: passed.

## Hands-on reader validation

- Application launch and ZIP image display.
- Best Fit.
- Right-to-left manga mode.
- Fullscreen by F/F11.
- Double-page display with no center gap.
- Arrow-key navigation.
- Left-half click advances; right-half click goes back.
- Compact thumbnail sidebar and thumbnail click navigation.
- Prioritized thumbnail loading with fast visible results.
- Manual 100% corrected for Windows 150% DPI scaling.
- Image dimensions and actual display zoom in the status bar.
- Automatic rollover to the next archive when scrolling past the end.
- Fast application shutdown.

## Installer / association validation

- Setup.exe installed successfully.
- Comet registration as an available ZIP/CBZ handler succeeded.
- Uninstall completed successfully.
- ZIP handling remained intact after uninstall.

## Follow-up

The following are intentionally tracked as non-blocking follow-up validation:
different-DPI multi-monitor transitions, explicit damaged-image navigation testing,
multi-book reading-position restoration testing, and additional image-format coverage.
