# Comet v1.0 requirements implementation status

> Current baseline: v0.2.0 released on 2026-09-19.  
> This matrix supersedes the old v0.1.0 implementation-status snapshot.

| ID | Requirement | Current status | Evidence / notes |
|---|---|---|---|
| FR-001 | Fast startup | Implemented + Windows validated | Self-contained WPF startup and first-render performance measured in v0.2.0. |
| FR-002 | Fast load | Implemented + Windows validated | First page prioritized; no whole-book extraction/decode prerequisite. |
| FR-003 | ZIP/CBZ direct reading | Implemented + validated | System.IO.Compression path retained. |
| FR-004 | Image-folder reading | Implemented | Direct folder and individual-image workflow. |
| FR-005 | File association | Implemented + validated | Optional installer registration; user controls Windows defaults. |
| FR-006 | Single page | Implemented + validated | Core reader mode. |
| FR-007 | Double page / cover alone | Implemented + validated | Cover alone; landscape-alone handling included. |
| FR-008 | Manga / RTL | Implemented + validated | Default ON; spread placement/navigation follow reading direction. |
| FR-009 | Best Fit | Implemented + validated | Default fit mode; small-image enlargement supported. |
| FR-010 | Fit Width | Implemented + validated | Viewport-based. |
| FR-011 | Fit Height | Implemented + validated | Viewport-based. |
| FR-012 | Manual Zoom | Implemented + validated | DPI-correct Manual 100% plus temporary zoom controls. |
| FR-013 | Smart Scroll | Implemented + validated | Scroll first, flip at edge, adjacent-archive rollover. |
| FR-014 | Previous/next archive | Implemented + validated | Extended beyond ZIP/CBZ to all supported comic archives. |
| FR-015 | Prefetch cache | Implemented + measured | Bounded LRU plus stale-load cancellation; v0.2.0 performance baseline recorded. |
| FR-016 | Natural sort | Implemented + automated coverage | Numeric and Unicode filenames covered. |
| FR-017 | Fullscreen | Implemented + validated | F/F11. |
| FR-018 | Thumbnails | Implemented + validated | Compact sidebar with prioritized loading. |
| FR-019 | Reading position | Implemented + validated | Per-book JSON; restart restore tested. |
| FR-020 | Bookmark | Implemented | Add/open bookmark list. |
| FR-021 | Settings persistence | Implemented | Fit, reading direction, layout/UI state. |
| FR-022 | Japanese/English UI | Implemented; v0.3 audit pending | Locale follows Windows UI culture; remaining literals to be audited. |
| FR-023 | MComix-compatible controls | Implemented for in-scope features | See KEYBINDINGS.md. |
| FR-024 | Drag and drop | Implemented | Archive, folder, supported image. |
| FR-025 | Damaged page continuation | Implemented + validated | Localized placeholder; forward/back navigation remains available. |

## Additional capabilities beyond the original v1.0 baseline

- WebP with self-contained SkiaSharp fallback.
- RAR/CBR.
- 7z/CB7.
- MComix-style detailed status bar.
- Performance tracing and summary tooling.
- JPEG header probing and optimized WIC decode path.
- Byte-aware image cache budgeting and stale-load cancellation.

## v0.3.0 audit focus

The feature matrix is effectively complete. v0.3.0 therefore focuses on:

- localization/UI literal audit,
- deterministic regression coverage,
- settings/state upgrade hardening,
- packaging/version consistency,
- performance/memory regression guardrails.

PDF, Library, slideshow, magnifier, and image enhancement remain deferred.
