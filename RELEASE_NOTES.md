# Comet v0.2.0

Comet v0.2.0 expands the reader beyond ZIP/CBZ while keeping the fast MComix-inspired
reading workflow established in v0.1.0.

Highlights:
- Direct ZIP/CBZ, RAR/CBR, and 7z/CB7 reading.
- Guaranteed WebP support without requiring the Microsoft Store WebP codec.
- Damaged-page resilience so a broken image does not block navigation.
- Reading-state validation so replaced archives do not inherit stale page positions.
- DPI-change hardening and DPI-correct Manual 100% behavior.
- MComix-style status bar with page range, source dimensions and actual zoom, archive
  name, page filenames, and source image sizes.
- JPEG/PNG/GIF/BMP source dimensions are read from image headers where possible,
  avoiding an expensive WIC probe path.
- Array-backed image data is passed to WIC without an unnecessary full copy.
- Decode-time upscaling beyond source width is avoided.
- Optional Windows file-association registration for ZIP/CBZ/RAR/CBR/7z/CB7.
- Self-contained Windows x64 portable ZIP and Inno Setup installer.

Performance field measurement on a Windows manga workload with typical 1619x2048 JPEG
pages showed average page decode improving from 38.10 ms to 10.72 ms, while render-page
P95 improved from 114.12 ms to 15.77 ms. These are field measurements rather than a
hardware-normalized benchmark.

v0.2.0 remains a pre-1.0 release. PDF support, library/database UI, slideshow, magnifier,
and image editing/enhancement remain outside this milestone.
