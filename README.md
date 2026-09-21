# Comet

Comet is a Windows comic viewer focused on the parts of the MComix reading experience that matter most for this project: **fast startup, direct comic-archive reading, reliable fit modes, temporary zoom, and right-to-left manga reading**.

> Status: **v1.0.2 hands-on validation passed on 2026-09-21; final release preparation is complete**.

## v1.0 focus

v1.0 promotes the Windows-validated v0.3.0 baseline to the first general-availability release. The original FR-001..FR-025 and NFR-001..NFR-010 scope is frozen, RC1 passed, and no broad feature expansion is being added before GA. See `docs/ROADMAP_v1.0.md` and `docs/RC_VALIDATION_v1.0.md`.

## Current reader scope

- Windows 11 x64, .NET 10 LTS / C# 14 / WPF.
- Open ZIP/CBZ directly through the .NET ZIP path.
- Open RAR/CBR and 7z/CB7 directly through SharpCompress.
- Open image folders directly; dropping an individual image opens its folder at that image.
- Natural filename sorting.
- Single-page and double-page display; cover page alone.
- Right-to-left manga mode enabled by default.
- Best Fit enabled by default; small images are enlarged by default.
- Fit Width, Fit Height, Manual 100%, temporary zoom, and left-button drag panning when the displayed content exceeds the viewport.
- Ctrl+mouse-wheel zoom remains active while moving to the previous/next archive in the same session, then resets on process restart.
- Basic smart scrolling and flip-at-edge behavior.
- Adjacent ZIP/CBZ/RAR/CBR/7z/CB7 navigation.
- Thumbnail-wheel scrolling stays inside the thumbnail sidebar; clicking a thumbnail jumps directly to that page.
- Right-clicking the main reader opens a context menu for navigation and common display actions.
- Reading-position persistence and bookmarks.
- Fullscreen, drag-and-drop, Japanese/English UI.
- Optional ZIP/CBZ/RAR/CBR/7z/CB7 file-association registration through the installer.

PDF and library management remain deferred. RAR/CBR and 7z/CB7 have passed Windows hands-on validation in v0.2.0; see `CHANGELOG.md` and `docs/ROADMAP_v0.2.0.md`.

## Build on Windows

Prerequisites:
- Windows 11 x64
- .NET 10 SDK

For a self-contained build:

```powershell
.\scripts\publish-win-x64.ps1 -Version 1.0.2
```

The publish output is written to `artifacts/win-x64`.

## Create release assets locally

Install Inno Setup 6, then run:

```powershell
.\scripts\package-release.ps1 -Version 1.0.2 -BuildInstaller
```

Assets are written to `artifacts/release` with SHA-256 hashes.

## GitHub release flow

The repository contains two workflows:
- `CI`: build + smoke tests + Windows x64 publish on pushes and pull requests.
- `Release`: triggered by `v*` tags; builds, tests, packages a portable ZIP and Setup.exe, calculates hashes, and creates a GitHub Release. Pre-release versions such as `0.x` or versions containing a hyphen are marked as prereleases; stable `1.x` versions such as `v1.0.2` are normal releases.

Repository operations and tagging are performed through GitHub Desktop. See `docs/RELEASE_CHECKLIST.md` before tagging.

## Core controls

The project deliberately follows MComix key behavior where the corresponding feature exists. Common controls include:

- `PageDown` / left click: next page or spread
- Left-button drag on oversized content: pan the displayed page/spread
- `PageUp` / Backspace: previous page or spread
- `Space` / wheel: smart scroll, then flip page at the edge
- `B`, `W`, `H`, `A`: Best Fit / Fit Width / Fit Height / Manual 100%
- `Ctrl+wheel`, `+`, `-`, `Ctrl+0`: temporary zoom
- `D`: double-page mode
- `M`: manga mode
- `F` / `F11`: fullscreen
- `Ctrl+Shift+N/P`: next/previous supported comic archive
- `Ctrl+D` / `Ctrl+B`: add/open bookmarks

Full table: `docs/KEYBINDINGS.md`.

## Architecture

```text
Comet.App (WPF UI, input, page cache)
       |               |
       v               v
Comet.Core       Comet.Platform.Windows
       |
       v
Comet.Infrastructure (comic archives, folders, settings, reading state)
```

The UI thread does not perform archive extraction or image decoding. Current-page work is prioritized and nearby pages are prefetched into a bounded LRU cache. The full-page cache is bounded by both item count and an estimated decoded-bitmap memory budget. Fit calculations use the actual image viewport.

For opt-in performance measurement, launch with `COMET_PERF=1`; results are written to `%LOCALAPPDATA%\Comet\logs\performance.log`. Run `.\scripts\summarize-performance.ps1` to summarize latency percentiles and cache hit rate.

See `docs/Comet_v1.0_Technical_Architecture_v0.1.md` for the full rationale.

## File associations

The installer can register Comet as an available handler for `.zip`, `.cbz`, `.rar`, `.cbr`, `.7z`, and `.cb7`. It does **not** silently replace the user's current default application. After installation, Windows Default Apps is opened so the user can select Comet. Once selected, double-clicking a supported archive opens it directly in Comet.

## Project relationship to MComix

Comet is an independent implementation. MComix is used as a behavioral/UX reference, especially for reading direction and keybindings. No MComix source code, artwork, or binaries are included, and Comet is not an official MComix port or affiliated project. See `NOTICE.md`.

## License

MIT. See `LICENSE`.
