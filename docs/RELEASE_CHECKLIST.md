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

## v0.2.0 implementation and hands-on validation

- [x] WebP display, thumbnails, portrait/landscape pages, and page turns.
- [x] Damaged page placeholder and continued forward/back navigation.
- [x] Reading-position persistence across full restart.
- [x] Replaced archive does not inherit stale reading state.
- [x] DPI refresh and Manual 100% regression coverage.
- [x] RAR/CBR direct reading.
- [x] Solid RAR/CBR handling.
- [x] 7z/CB7 direct reading.
- [x] Solid 7z/CB7 handling.
- [x] Adjacent-archive navigation for ZIP/CBZ/RAR/CBR/7z/CB7.
- [x] Optional Windows associations for all supported archive formats.
- [x] MComix-style status details: page range, source dimensions/zoom, archive, image name, source size.
- [x] JPEG/PNG/GIF/BMP fast source-dimension header probing.
- [x] Avoid decode-time upscaling beyond source width.
- [x] JPEG tuning field measurement completed.
- [x] page.decode average improved from 38.10 ms to 10.72 ms on the measured workload.
- [x] render.page P95 improved from 114.12 ms to 15.77 ms on the measured workload.
- [x] Final implementation CI green through the JPEG tuning phase.

## v0.2.0 RC gate

- [x] RC2 memory-pressure check on the same 844x1200 double-page ZIP: about 200 MiB idle, 600 MiB peak during rapid paging, 250-350 MiB after stopping, with no perceived speed regression.

- [ ] Build the final v0.2.0 RC package from the release-preparation commit.
- [ ] Install the RC with Setup.exe.
- [ ] Confirm ZIP/CBZ/RAR/CBR/7z/CB7 are offered as optional handlers.
- [ ] Open representative ZIP/CBZ/CBR/CB7 books from the installed build.
- [x] Re-check page turns, thumbnails, fullscreen, double-page, manga mode, and fit modes.
- [x] Re-check reading-position restore after restart.
- [x] Confirm the MComix-style status bar values are correct and visually spaced close to MComix.
- [x] Confirm fast shutdown with no lingering Comet process.
- [ ] Uninstall successfully.
- [ ] Confirm existing archive handling remains intact after uninstall.
- [ ] Confirm release ZIP and installer are not corrupted.

## Tag and release

After the RC gate is complete and the final release-preparation commit is synced locally,
create the v0.2.0 tag:

```powershell
git tag v0.2.0
git push origin v0.2.0
```

The Release workflow builds and uploads:
- `Comet-v0.2.0-win-x64.zip`
- `Comet-v0.2.0-win-x64-Setup.exe`
- `SHA256SUMS.txt`

Because Comet is still pre-1.0, the workflow creates v0.2.0 as a GitHub prerelease.
