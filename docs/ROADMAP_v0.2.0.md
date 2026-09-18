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

3. **Reading-state validation — in progress**
   - JSON round-trip coverage already verifies last-page and bookmark persistence.
   - Source size/last-write metadata is now validated before restoring state.
   - If a ZIP/CBZ is replaced at the same path, stale reading position/bookmarks are discarded.
   - Hands-on restart/reopen validation remains before this item is complete.

4. **DPI / multi-monitor hardening**
   - Re-check Best Fit, Fit Width, Fit Height, Manual 100%, sidebar toggling, and fullscreen
     when moving between monitors with different Windows scaling.

5. **Archive-format expansion**
   - Evaluate RAR/CBR and 7z/CB7 only after the above are stable.
   - PDF remains a separate decode path and is lower priority than archive-image formats.

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
