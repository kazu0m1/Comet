# Comet v0.2.0 roadmap

> Baseline: v0.1.0 release completed and validated on Windows 11.

## Goal

Move Comet from a solid ZIP/CBZ reader toward the v1.0 requirements baseline without
sacrificing the responsiveness established in v0.1.0.

## Priority order

1. **WebP support — implemented and Windows validated**
   - WPF/WIC remains the primary path for existing formats.
   - WebP uses a narrowly scoped SkiaSharp fallback and does not depend on the
     Microsoft Store WebP codec being installed.
   - Windows CI covers WebP probe and decode-to-size.
   - Windows 11 hands-on validation passed for ZIP display, thumbnails, portrait/landscape pages, and page turns.

2. **Damaged-page resilience — implemented and Windows validated**
   - A failed page shows a localized placeholder explaining that navigation can continue.
   - Thumbnail selection and reading-state updates remain usable on the failed page.
   - Windows CI covers a ZIP sequence with valid image -> broken image -> valid image and confirms the page after the failure still decodes.
   - Windows 11 hands-on validation passed for error placeholder and continued forward/back navigation.

3. **Reading-state validation — implemented and Windows validated**
   - JSON round-trip coverage verifies last-page and bookmark persistence.
   - Source size/last-write metadata is validated before restoring state.
   - If an archive is replaced at the same path, stale reading position/bookmarks are discarded.
   - Windows 11 restart/reopen validation passed.

4. **DPI / multi-monitor hardening — implemented and Windows validated**
   - Manual 100% and physical zoom calculations are centralized and CI-tested at 100/125/150/200%.
   - DPI changes explicitly refresh and re-render the viewport.
   - Windows hands-on validation passed.

5. **Performance diagnostics / JPEG tuning — implemented and Windows validated**
   - Set `COMET_PERF=1` to record source-open, first-render, page-read, decode, cache hit/miss, and DPI-change events.
   - `scripts/summarize-performance.ps1` reports count/average/P50/P95/max plus cache hit rate.
   - JPEG/PNG/GIF/BMP source dimensions are probed from headers where possible.
   - Array-backed page bytes are passed to WIC without an extra full copy.
   - Decode-time upscaling beyond source width is avoided.
   - Windows field measurement on typical 1619x2048 JPEG pages reduced page-decode average from 38.10 ms to 10.72 ms and render-page P95 from 114.12 ms to 15.77 ms.
   - Cache/prefetch experiments that worsened responsiveness were reverted; CI #45 page-turn behavior remains the baseline.

6. **Archive-format expansion — implemented and Windows validated**
   - RAR/CBR and 7z/CB7 use SharpCompress 0.50.4 while ZIP/CBZ stays on the existing System.IO.Compression path.
   - Natural ordering, image filtering, direct entry streaming, adjacent-archive navigation, Open dialog discovery, and optional Windows associations are wired for the new formats.
   - CI covers normal and solid archive cases, including out-of-order last-page reads.
   - Windows hands-on validation passed for normal/solid CBR and solid CB7.
   - PDF remains a separate decode path and is deferred beyond the current v0.2.0 image-archive milestone.

7. **MComix-style status information — implemented**
   - Status bar shows page range, per-page source dimensions and actual zoom, archive name, page filenames, and source image sizes.

## Non-goals for v0.2.0

- Library/database UI.
- Image editing/enhancement.
- Slideshow or magnifier.
- Replacing WPF rendering wholesale.
- PDF support.

## Acceptance targets

- WebP pages work inside ZIP/CBZ and folders on a clean Windows system without requiring
  a separately installed WebP Store codec.
- RAR/CBR and 7z/CB7 open directly and remain navigable.
- Existing JPEG/PNG startup and navigation behavior are not regressed.
- A broken page does not block moving to the next/previous valid page.
- Reading position round-trips across reopen in automated tests and hands-on validation.
- JPEG page-turn tail latency is materially better than the pre-tuning baseline.
- v0.2.0 remains self-contained win-x64 and installable with the existing Inno Setup path.

## Current status

All planned v0.2.0 feature/hardening work above is implemented and hands-on validated.
The next phase is release-candidate packaging and final install/uninstall regression validation.
