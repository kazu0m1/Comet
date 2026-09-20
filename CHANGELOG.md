# Changelog

All notable changes to Comet are documented here.

## [1.0.1] - 2026-09-20

### Added
- Left-button drag panning for a page or spread when the rendered content is larger than the viewport.
- Drag gestures use the Windows drag-distance threshold so ordinary left clicks continue to perform the existing click-zone page navigation.
- Panning remains bounded to the rendered content and is disabled when the content already fits inside the viewport.

### Validation
- Added smoke coverage for click-versus-drag thresholding, non-pannable content, drag deltas, and click suppression after a drag.
- Full CI build, smoke tests, Windows x64 publish, and installer packaging passed without requiring a separate hands-on gate.

## [1.0.0] - 2026-09-19

### General availability
- Promoted the fully audited v0.3.0 baseline to the first general-availability release.
- Frozen v1.0 baseline covers FR-001..FR-025 and NFR-001..NFR-010 with no missing core requirement identified.
- v1.0.0-rc1 passed the final Windows hands-on gate, including install/launch, core reading workflow, zoom/session/restart behavior, reading-position restore, CBR/CB7 regression checks, localization, uninstall, and file-association health.
- RC2 was not required because RC1 exposed no release-blocking defect requiring code or packaging changes.
- Final Windows x64 portable ZIP and Inno Setup packaging are versioned 1.0.0.

## [0.3.0] - 2026-09-19

### Added
- Formal FR-001..FR-025 and NFR-001..NFR-010 implementation audit with a v0.3.0 regression matrix.
- Expanded smoke coverage for Unicode image folders, individual-image routing, all declared archive/image extensions, and adjacent archive ordering across ZIP/CBZ/RAR/CBR/7z/CB7.
- English/Japanese localization parity checks in CI.
- Persistent thumbnail-sidebar width after splitter resize.

### Changed
- Invalid persisted enum/numeric settings are normalized to safe values before use.
- Settings and reading-state temp files are cleaned up on interrupted writes on a best-effort basis.
- Bookmark action text, open-failure messaging, and reader tooltips are fully routed through localization.
- CI development artifacts and fallback package defaults now use `0.3.0-dev`.
- Release documentation uses GitHub Desktop for repository/tag operations.


## [0.2.0] - 2026-09-19

### Added
- Darker cobalt-blue application icon for stronger visibility in Windows file associations and small icon sizes.
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
