# Comet v0.2.0 roadmap

> Baseline: v0.1.0 release completed and validated on Windows 11.

## Goal

Move Comet from a solid ZIP/CBZ reader toward the v1.0 requirements baseline without
sacrificing the responsiveness established in v0.1.0.

## Priority order

1. **WebP support**
   - Keep WPF/WIC for existing formats.
   - Add a narrowly scoped WebP fallback decoder so support does not depend on the
     Microsoft Store WebP codec being installed.
   - Preserve decode-to-size behavior where practical and avoid slowing JPEG/PNG.

2. **Damaged-page resilience**
   - Make a failed page visibly skippable rather than only surfacing an error.
   - Add integration coverage with a ZIP that contains a broken image between valid pages.

3. **Reading-state validation**
   - Add repeatable tests for last-page restoration and bookmarks across reopen/restart.
   - Keep per-book JSON; no database is introduced.

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
