# Changelog

All notable changes to Comet are documented here.

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

### Known limitations
- Thumbnail sidebar is intentionally deferred to the next milestone to keep the
  first functional build focused on startup, archive loading, display, fit, zoom,
  and navigation performance.
- RAR/CBR, 7z/CB7, and PDF are not supported in v0.1.0.
- The generated repository has been statically checked in the authoring
  environment, but the final Windows build gate is the included GitHub Actions
  workflow or a local Windows 11 + .NET 10 SDK build.
