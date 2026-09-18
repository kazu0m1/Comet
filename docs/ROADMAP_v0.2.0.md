# Comet v0.2.0 roadmap

> Baseline: v0.1.0 release completed and validated on Windows 11.

## Goal

Move Comet from a solid ZIP/CBZ reader toward the v1.0 requirements baseline without
sacrificing the responsiveness established in v0.1.0.

## Priority order

1. **WebP support — implemented**
   - WPF/WIC remains the primary path for existing formats.
   - WebP uses a narrowly scoped SkiaSharp fallback and does not depend on the
     Microsoft Store WebP codec being installed.
   - Windows CI covers WebP probe and decode-to-size.
   - Windows 11 hands-on validation passed for ZIP display, thumbnails, portrait/landscape pages, and page turns.

2. **Damaged-page resilience — implemented and Windows validated**
   - A failed page shows a localized placeholder explaining that navigation can continue.
   - Thumbnail selection and reading-state updates remain usable on the failed page.
   - Windows CI covers a ZIP sequence with valid image → broken image → valid image and confirms the page after the failure still decodes.
   - Windows 11 hands-on validation passed for error placeholder and continued forward/back navigation.

3. **Reading-state validation — implemented and Windows validated**
   - JSON round-trip coverage verifies last-page and bookmark persistence.
   - Source size/last-write metadata is validated before restoring state.
   - If a ZIP/CBZ is replaced at the same path, stale reading position/bookmarks are discarded.
   - Windows 11 restart/reopen validation passed.

4. **DPI / multi-monitor hardening — implemented and Windows validated**
   - Manual 100% and physical zoom calculations are centralized and CI-tested at 100/125/150/200%.
   - DPI changes explicitly refresh and re-render the viewport.
   - Windows hands-on validation passed.

5. **Performance diagnostics / cache hardening — active optimization**
   - Set `COMET_PERF=1` to record source-open, first-render, page-read, decode, cache hit/miss, and DPI-change events.
   - Diagnostics are disabled by default and write to `%LOCALAPPDATA%\Comet\logs\performance.log`.
   - `scripts/summarize-performance.ps1` reports count/average/P50/P95/max plus cache hit rate.
   - The full-page LRU uses both a six-item cap and an estimated 192 MiB decoded-bitmap budget, retaining at least the most-recent page.
   - Logging failures are swallowed and never affect reading.
   - First Windows field measurement: first render ~36 ms average, render-page median 1.9 ms, P95 ~114 ms.
   - Main-page, page-prefetch, and thumbnail traces are separated.
   - Field measurement showed worse subjective responsiveness despite a 1.35 ms render median: page decode rose to ~62 ms average and render P95 to ~126 ms.
   - Follow-up change prioritizes demand rendering: stale prefetch queues are cancelled, prefetch is sequential, page prefetch depth is reduced, thumbnail decode concurrency is capped at one, and recurrent thumbnail neighborhood warmup is removed.

6. **Archive-format expansion — implemented and Windows validated**
   - RAR/CBR and 7z/CB7 use SharpCompress 0.50.4 while ZIP/CBZ stays on the existing System.IO.Compression path.
   - Natural ordering, image filtering, direct entry streaming, adjacent-archive navigation, Open dialog discovery, and optional Windows associations are wired for the new formats.
   - 7z/CB7 has CI-generated archive coverage.
   - RAR/CBR uses a real RAR fixture from the SharpCompress MIT test suite for factory routing, image filtering, entry reading, and JPEG decode in CI.
   - CI also exercises solid RAR/CBR and solid 7z/CB7 fixtures by reading the last image entry first.
   - Windows hands-on validation passed for normal/solid CBR and solid CB7.
   - PDF remains a separate decode path and is deferred beyond the current v0.2.0 image-archive milestone.

## Non-goals for v0.2.0

- Library/database UI.
- Image editing/enhancement.
- Slideshow or magnifier.
- Replacing WPF rendering wholesale.

## Acceptance targets

- WebP pages work inside ZIP/CBZ and folders on a clean Windows system without requiring
  a separately installed WebP Store codec.
- Existing JPEG/PNG startup and navigation behavior are not regressed.
- A broken page does not block moving to the next/previous valid page.
- Reading position round-trips across reopen in automated tests and hands-on validation.
- v0.2.0 remains self-contained win-x64 and installable with the existing Inno Setup path.


## Performance rollback decision

The page/thumbnail separation and foreground-priority experiments after CI #45 were reverted after Windows hands-on testing showed worse subjective responsiveness and a render-page median regression from 1.9 ms to about 60 ms.

The runtime behavior is restored to the CI #45 baseline. Performance tracing, CMD/PowerShell measurement helpers, and the recorded baseline remain available for diagnostics, but no further cache/prefetch tuning is applied before the v0.2.0 release candidate unless a reproducible defect appears.


## JPEG decode tuning phase

Windows field testing identified JPEG bitmap decode, not archive I/O, as the remaining cold-page cost for typical 1619x2048 pages around 0.5-1 MiB. The first low-risk tuning step avoids WIC `OnLoad` for source-dimension probing by reading JPEG/PNG/GIF/BMP dimensions directly from headers, and avoids an extra byte-array copy when the source memory is already array-backed.

Performance tracing now records `page.probe` and `page.bitmap-decode` separately while retaining `page.decode` as the end-to-end decode metric. The MComix-style status bar is also expanded to show page numbers, source dimensions plus actual zoom, archive name, page filenames, and source image sizes.
