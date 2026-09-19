# Comet v1.0 requirements implementation status

> Current baseline: v0.2.0 released on 2026-09-19.  
> v0.3.0 is auditing and locking this baseline rather than adding another major subsystem.

## Functional requirements

| ID | Requirement | Current status | Evidence / notes |
|---|---|---|---|
| FR-001 | Fast startup | Implemented + Windows validated | Self-contained WPF startup and first-render performance measured in v0.2.0. |
| FR-002 | Fast load | Implemented + Windows validated | First page prioritized; no whole-book extraction/decode prerequisite. |
| FR-003 | ZIP/CBZ direct reading | Implemented + automated + validated | System.IO.Compression path retained. |
| FR-004 | Image-folder reading | Implemented + automated | Folder filtering, Unicode natural order, page reads, and individual-image routing covered. |
| FR-005 | File association | Implemented + Windows validated | Optional installer registration; user controls Windows defaults. |
| FR-006 | Single page | Implemented + validated | Core reader mode. |
| FR-007 | Double page / cover alone | Implemented + automated + validated | Spread index planning covered; landscape-alone behavior remains a visual/manual check. |
| FR-008 | Manga / RTL | Implemented + validated | Default ON; spread placement/navigation follow reading direction. |
| FR-009 | Best Fit | Implemented + automated + validated | Fit calculation covered; small-image enlargement supported. |
| FR-010 | Fit Width | Implemented + automated + validated | Viewport-based calculation. |
| FR-011 | Fit Height | Implemented + automated + validated | Viewport-based calculation. |
| FR-012 | Manual Zoom | Implemented + automated + validated | DPI-correct Manual 100% plus session zoom behavior covered. |
| FR-013 | Smart Scroll | Implemented + Windows validated | Scroll first, flip at edge, adjacent-archive rollover. |
| FR-014 | Previous/next archive | Implemented + automated + validated | All ZIP/CBZ/RAR/CBR/7z/CB7 extensions covered in natural order. |
| FR-015 | Prefetch cache | Implemented + automated + measured | Bounded weighted LRU, stale-load cancellation, and v0.2.0 performance baseline. |
| FR-016 | Natural sort | Implemented + automated | Numeric and Unicode filenames covered. |
| FR-017 | Fullscreen | Implemented + Windows validated | F/F11. |
| FR-018 | Thumbnails | Implemented + Windows validated | Compact sidebar with prioritized loading. |
| FR-019 | Reading position | Implemented + automated + validated | JSON round-trip/source validation plus restart hands-on check. |
| FR-020 | Bookmark | Implemented + automated persistence coverage | Bookmark JSON round-trip and dialog flow implemented. |
| FR-021 | Settings persistence | Implemented + automated | Round-trip, migration, corrupt fallback, and invalid-value normalization covered. |
| FR-022 | Japanese/English UI | Implemented; audit in progress | Locale follows Windows UI culture; bookmark action and open-failure text localized in v0.3.0. |
| FR-023 | MComix-compatible controls | Implemented for in-scope features | See KEYBINDINGS.md; core behavior Windows-validated. |
| FR-024 | Drag and drop | Implemented | Archive, folder, supported image. |
| FR-025 | Damaged page continuation | Implemented + automated + validated | Valid → broken → valid sequence covered; localized continuation UI validated. |

## Non-functional requirements

| ID | Requirement | Current status | Evidence / notes |
|---|---|---|---|
| NFR-001 | Responsiveness first | Implemented + measured | JPEG decode average 10.72 ms and render-page P95 15.77 ms on the established v0.2.0 field workload. |
| NFR-002 | Async I/O/decode | Implemented | Archive reads/decode/thumbnail work are kept off the UI thread; UI awaits results. |
| NFR-003 | Memory bound | Implemented + Windows observed | Six-item/192 MiB decoded-bitmap budget plus stale-load cancellation; RC2 peak observation about 600 MiB process memory during rapid paging. |
| NFR-004 | DPI support | Implemented + automated + validated | Manual 100% tested at 100/125/150/200%; DPI changes re-render. |
| NFR-005 | Viewport-based fit | Implemented + automated | Fit calculator consumes actual viewport dimensions. |
| NFR-006 | Settings fault tolerance | Implemented + automated | Missing/corrupt/invalid values fall back or normalize safely; temp-write cleanup added in v0.3.0. |
| NFR-007 | Distributable Windows artifacts | Implemented + release validated | Self-contained ZIP + Inno Setup installer produced by CI/Release workflows. |
| NFR-008 | Diagnostics | Implemented | Opt-in performance tracing and summary script. |
| NFR-009 | Unicode | Implemented + automated | Unicode natural filenames and Unicode folder paths covered. |
| NFR-010 | MComix non-interference | Implemented | Independent app/config/AppId; no MComix source, settings, artwork, or installation changes. |

## Additional capabilities beyond the original v1.0 baseline

- WebP with self-contained SkiaSharp fallback.
- RAR/CBR.
- 7z/CB7.
- MComix-style detailed status bar.
- Performance tracing and summary tooling.
- JPEG header probing and optimized WIC decode path.
- Byte-aware image cache budgeting and stale-load cancellation.

## Audit conclusion so far

No missing core v1.0 feature has been found. The remaining v0.3.0 work is primarily
regression locking, final localization/UI review, state/packaging consistency, and a short
Windows hands-on gate before deciding whether to move directly to a v1.0 release-candidate cycle.
