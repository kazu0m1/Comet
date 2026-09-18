# Changelog

All notable changes to Comet are documented here.

## [Unreleased] — v0.2.0

### Added
- RC2 memory-pressure hardening: stale page loads and superseded prefetch batches are cancelled once no active viewer is waiting for them, without reducing normal prefetch parallelism.
- MComix-style status-bar spacing and visual left-to-right spread detail ordering in manga mode.
- MComix-style status-bar details: page range, per-page source dimensions and actual zoom, archive name, page filenames, and source image sizes.
- Fast JPEG/PNG/GIF/BMP header probing for source dimensions plus zero-copy WIC input streams when page bytes are array-backed.
- Separate `page.probe` and `page.bitmap-decode` performance metrics for JPEG tuning.
- JPEG field tuning reduced measured average page decode from 38.10 ms to 10.72 ms and render-page P95 from 114.12 ms to 15.77 ms on the measured Windows workload.
- Guaranteed WebP page support through a SkiaSharp fallback decoder, including decode-to-size CI coverage and Windows 11 hands-on validation.
- Localized damaged-page placeholder that keeps previous/next navigation usable, validated on Windows 11.
- Reading-state source validation so replaced archives do not inherit stale page positions or bookmarks, with Windows restart/reopen validation.
- Explicit DPI-change refresh/re-render handling plus CI coverage for 100/125/150/200% Manual 100% calculations.
- Opt-in performance diagnostics for source open, first render, page read/decode, cache hit/miss, and DPI changes, plus a PowerShell summary script.
- Byte-aware full-page LRU budgeting (192 MiB default in addition to the six-item cap) to limit decoded-image memory pressure.
- Performance experiments after the CI #45 baseline were reverted after Windows hands-on testing showed worse page-turn responsiveness; the faster CI #45 cache/prefetch behavior is retained for v0.2.0.
- RAR/CBR and 7z/CB7 direct-reading infrastructure via SharpCompress 0.50.4 while preserving the existing ZIP/CBZ fast path.
- Optional Windows associations and adjacent-archive navigation for RAR/CBR/7z/CB7.
- Real RAR fixture coverage for CBR factory routing, image extraction, and JPEG decode.
- Solid RAR/CBR and solid 7z/CB7 fixture coverage, including out-of-order last-page reads, followed by Windows hands-on validation.
- Completed localization of primary reader chrome and the Open dialog file-type labels.
- Expanded regression coverage for zoom-session lifecycle, reading-state persistence, settings migration/corrupt-file fallback, Unicode natural ordering, and adjacent archive navigation.
- v0.2.0 hardening roadmap.

## [0.1.0] - 2026-09-18

### Added
- Windows WPF reader shell on .NET 10.
- Direct ZIP/CBZ reading without extracting the archive to disk.
- Direct image-folder reading.
- Natural page sorting.
- Single-page and double-page layouts with cover-alone behavior.
- Right-to-left manga mode, enabled by default.
- Best Fit, Fit Width, Fit Height, Manual 100% mode, and temporary zoom.
- Ctrl+mouse-wheel, plus/minus, and Ctrl+0 zoom controls.
- MComix-compatible core navigation keys for implemented features.
- Basic smart scrolling and page-flip-at-edge behavior.
- Previous/next ZIP or CBZ navigation while retaining temporary zoom.
- Reading-position persistence.
- Bookmark creation and bookmark navigation dialog.
- Fullscreen mode.
- Drag-and-drop for ZIP/CBZ, supported images, and folders.
- Japanese/English UI selected from the Windows UI culture.
- Windows file-association registration through the installer.
- GitHub Actions CI and tag-based release packaging.
- MComix-style thumbnail sidebar with compact, prioritized asynchronous loading.
- Arrow-key and click-zone page navigation.
- Automatic rollover to adjacent ZIP/CBZ archives at the reading edge.
- DPI-correct Manual 100% display.
- Image dimensions and actual display zoom in the status bar.
- Fast shutdown that does not wait on archive cleanup.
- End-user packages omit the .NET `createdump.exe` diagnostic helper.

### Known limitations
- WebP, RAR/CBR, 7z/CB7, and PDF are not yet supported.
- Core reading UX, installer compilation, install/uninstall, and ZIP/CBZ handler
  registration have passed Windows x64 CI and Windows 11 hands-on validation.
