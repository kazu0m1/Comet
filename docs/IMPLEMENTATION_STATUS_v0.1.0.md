# Comet v0.1.0 implementation status

This document maps the v1.0 requirements baseline to the first release candidate source tree.

| Requirement | v0.1.0 | Notes |
|---|---|---|
| Fast startup / first display | Implemented architecture | No eager archive extraction or all-page decode; Windows benchmark still required. |
| ZIP/CBZ direct reading | Implemented | `ZipArchive`, no filesystem extraction. |
| Image folder reading | Implemented | Top-level supported images, natural order. |
| ZIP/CBZ file association | Implemented installer registration | User must choose Comet in Windows Default Apps. |
| Single page | Implemented | Toggle from double-page mode. |
| Double page / cover alone | Implemented | Cover page 0 is alone; landscape pages are displayed alone. |
| Manga / right-to-left | Implemented | Default ON; spread placement and Alt+arrow direction follow reading direction. |
| Best Fit | Implemented | Default; small images stretch by default. |
| Fit Width / Fit Height | Implemented | Viewport-based scaling. |
| Manual Zoom | Implemented + Windows tested | DPI-correct physical-pixel 100% mode plus temporary zoom controls. |
| Ctrl+mouse-wheel temporary zoom | Implemented | Persists to adjacent archives, resets after process restart. |
| Smart Scroll | Implemented + Windows tested | Reading-flow scrolling, page flip at edge, and adjacent-archive rollover. |
| Previous/next archive | Implemented + Windows tested | Ctrl+Shift+P/N and reading-edge rollover, natural archive sort. |
| Bounded prefetch cache | Implemented | 6-item LRU, nearby pages prefetched. |
| Natural sort | Implemented + smoke test | Numeric runs sorted naturally. |
| Fullscreen | Implemented + Windows tested | F/F11. |
| Thumbnail sidebar | Implemented + Windows tested | Compact MComix-style sidebar, visible-neighborhood priority loading. |
| Reading position | Implemented | Per source JSON in LocalAppData. |
| Bookmark | Implemented | Add and open bookmark list. |
| Settings | Implemented | Base fit, reading direction, layout and stretch settings. |
| Japanese/English UI | Implemented | Selected from Windows UI culture. |
| MComix-compatible controls | Implemented for included features | See `KEYBINDINGS.md`. |
| Drag and drop | Implemented | Archive, folder, supported image. |
| Damaged page resilience | Implemented path | Failed page shows an error; navigation remains available. Requires Windows validation. |

## Build-validation state

Completed:
- Windows GitHub Actions restore/build/smoke-test/self-contained publish.
- Windows 11 hands-on validation of startup, ZIP image display, Best Fit, manga mode,
  fullscreen, thumbnail sidebar, keyboard/click navigation, Manual 100%, fast shutdown,
  center-gap removal, and adjacent-archive rollover.
- Repeated CI packaging of the portable Windows x64 build.

Release-candidate gate completed:
- Inno Setup installer compiled in CI.
- Installer completed successfully on Windows 11.
- Comet registered as an available ZIP/CBZ handler.
- Uninstall completed successfully.
- Existing ZIP handling remained intact after uninstall.

Remaining non-blocking follow-up tests are tracked in `docs/RELEASE_CHECKLIST.md`.
