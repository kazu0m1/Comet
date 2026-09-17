# Release checklist

## Before tagging

- [ ] Run `./scripts/verify.ps1` on Windows 11.
- [ ] Open a representative JPG/PNG ZIP and CBZ.
- [ ] Confirm cold start and first-page load are subjectively responsive.
- [ ] Confirm Best Fit after resizing the window and moving between different DPI monitors.
- [ ] Confirm right-to-left double-page ordering.
- [ ] Confirm Ctrl+mouse-wheel zoom persists to next/previous archive but resets after restarting Comet.
- [ ] Confirm reading position is restored after restart.
- [ ] Confirm a damaged image does not prevent page navigation.
- [ ] Install with the generated Setup.exe and register ZIP/CBZ associations.
- [ ] In Windows Default Apps, choose Comet for `.zip`/`.cbz`, then confirm double-click opening.
- [ ] Uninstall and confirm Comet-specific registration is removed without disturbing other ZIP handlers.
- [ ] Update `CHANGELOG.md` and `RELEASE_NOTES.md`.

## Tag and release

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The `Release` workflow builds and uploads:
- `Comet-v0.1.0-win-x64.zip`
- `Comet-v0.1.0-win-x64-Setup.exe`
- `SHA256SUMS.txt`

The workflow creates a GitHub prerelease automatically.
