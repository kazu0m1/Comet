# Release checklist

## v0.1.0 validation completed

- [x] GitHub Actions restore/build on Windows.
- [x] Smoke tests.
- [x] Self-contained Windows x64 publish.
- [x] Inno Setup compilation.
- [x] Cold start and ZIP first-page display on Windows 11.
- [x] Best Fit, manga/right-to-left mode, fullscreen, and double-page reading.
- [x] Arrow-key and left/right click-zone navigation.
- [x] Compact thumbnail sidebar with prioritized loading.
- [x] DPI-correct Manual 100% on a Windows display using 150% scaling.
- [x] Image dimensions and actual zoom display in the status bar.
- [x] Adjacent ZIP/CBZ rollover at the reading edge.
- [x] Fast close without a lingering Comet process.
- [x] Install with the generated Setup.exe.
- [x] Register Comet as an available ZIP/CBZ handler.
- [x] Uninstall successfully.
- [x] Confirm ZIP handling remains intact after uninstall.

## Follow-up validation after v0.1.0

- [ ] Move the running app between monitors with different DPI scaling and re-check all fit modes.
- [ ] Confirm temporary Ctrl+mouse-wheel zoom persists across adjacent archives and resets after a full restart.
- [ ] Confirm reading position restoration across restart with several books.
- [ ] Confirm a deliberately damaged image does not prevent navigation.
- [ ] Expand file-format coverage beyond JPEG/PNG where appropriate.

## Tag and release

After syncing the final release-preparation commit locally:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The `Release` workflow builds and uploads:
- `Comet-v0.1.0-win-x64.zip`
- `Comet-v0.1.0-win-x64-Setup.exe`
- `SHA256SUMS.txt`

Because Comet is still pre-1.0, the workflow creates v0.1.0 as a GitHub prerelease.
