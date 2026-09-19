# Comet v1.0.0

Comet v1.0.0 is the first general-availability release of Comet, a Windows comic viewer focused on fast direct archive reading and an MComix-inspired reading workflow.

Highlights:
- Fast direct ZIP/CBZ reading without pre-extraction, plus direct image-folder reading.
- Single-page and double-page reading with cover-alone behavior and right-to-left manga mode.
- Best Fit, Fit Width, Fit Height, DPI-correct Manual 100%, temporary zoom, and Smart Scroll.
- Adjacent archive navigation with session zoom retention.
- Asynchronous prefetch, bounded image cache, responsive thumbnails, reading-position persistence, bookmarks, fullscreen, and drag-and-drop.
- Japanese/English UI and optional Windows file-association registration.
- Damaged-page continuation instead of failing the entire reading session.
- WebP, RAR/CBR, and 7z/CB7 support retained as validated capabilities beyond the original v1.0 baseline.
- Self-contained Windows x64 portable ZIP and Inno Setup installer.

The v1.0 requirements baseline covers FR-001 through FR-025 and NFR-001 through NFR-010. The v1.0.0-rc1 Windows hands-on gate passed on 2026-09-19 without a release-blocking defect, so RC2 was not required.

The established v0.2.0 JPEG performance/cache path and v0.3.0 state, localization, regression, and packaging hardening are retained unchanged.

PDF, library/database UI, slideshow, magnifier, and image editing/enhancement remain outside v1.0.
