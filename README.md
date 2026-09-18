# Comet

Comet is a Windows comic viewer focused on the parts of the MComix reading experience that matter most for this project: **fast startup, direct ZIP/CBZ reading, reliable fit modes, temporary zoom, and right-to-left manga reading**.

> Status: **v0.1.0 released; v0.2.0 hardening in development**. The repository is structured for repeatable Windows CI, installer validation, and tagged prereleases.

## v0.2.0 development focus

v0.2.0 hardens the existing reader before broader format expansion: regression tests for state/zoom/navigation, damaged-page resilience, DPI/resize validation, cache/performance diagnostics, localization cleanup, and a WebP codec decision. See `docs/ROADMAP_v0.2.0.md`.

## v0.1.0 scope

- Windows 11 x64, .NET 10 LTS / C# 14 / WPF.
- Open ZIP and CBZ without extracting them to disk.
- Open image folders directly; dropping an individual image opens its folder at that image.
- Natural filename sorting.
- Single-page and double-page display; cover page alone.
- Right-to-left manga mode enabled by default.
- Best Fit enabled by default; small images are enlarged by default.
- Fit Width, Fit Height, Manual 100%, temporary zoom.
- Ctrl+mouse-wheel zoom remains active while moving to the previous/next ZIP in the same session, then resets on process restart.
- Basic smart scrolling and flip-at-edge behavior.
- Adjacent ZIP/CBZ navigation.
- Reading-position persistence and bookmarks.
- Fullscreen, drag-and-drop, Japanese/English UI.
- Optional ZIP/CBZ file-association registration through the installer.

RAR/7z/PDF and library management remain intentionally deferred; see `CHANGELOG.md`, `docs/ROADMAP_v0.2.0.md`, and the v1.0 requirements in `docs/`.

## Build on Windows

Prerequisites:
- Windows 11 x64
- .NET 10 SDK

```powershell
git clone <your-repository-url>
cd Comet
.\scripts\verify.ps1
```

For a self-contained build:

```powershell
.\scripts\publish-win-x64.ps1 -Version 0.1.0
```

The publish output is written to `artifacts/win-x64`.

## Create release assets locally

Install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then run:

```powershell
.\scripts\package-release.ps1 -Version 0.1.0 -BuildInstaller
```

Assets are written to `artifacts/release` with SHA-256 hashes.

## GitHub release flow

The repository contains two workflows:
- `CI`: build + smoke tests + Windows x64 publish on pushes and pull requests.
- `Release`: triggered by `v*` tags; builds, tests, packages a portable ZIP and Setup.exe, calculates hashes, and creates a GitHub prerelease.

See `docs/RELEASE_CHECKLIST.md` before tagging.

## Core controls

The project deliberately follows MComix key behavior where the corresponding feature exists. Common controls include:

- `PageDown` / left click: next page or spread
- `PageUp` / Backspace: previous page or spread
- `Space` / wheel: smart scroll, then flip page at the edge
- `B`, `W`, `H`, `A`: Best Fit / Fit Width / Fit Height / Manual 100%
- `Ctrl+wheel`, `+`, `-`, `Ctrl+0`: temporary zoom
- `D`: double-page mode
- `M`: manga mode
- `F` / `F11`: fullscreen
- `Ctrl+Shift+N/P`: next/previous ZIP or CBZ
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
Comet.Infrastructure (ZIP/CBZ, folders, settings, reading state)
```

The UI thread does not perform ZIP extraction or image decoding. Current-page work is prioritized and nearby pages are prefetched into a bounded LRU cache. Fit calculations use the actual image viewport.

See `docs/Comet_v1.0_Technical_Architecture_v0.1.md` for the full rationale.

## File associations

The installer can register Comet as an available handler for `.zip` and `.cbz`. It does **not** silently replace the user's current default application. After installation, Windows Default Apps is opened so the user can select Comet. Once selected, double-clicking a ZIP/CBZ opens it directly in Comet.

## Project relationship to MComix

Comet is an independent implementation. MComix is used as a behavioral/UX reference, especially for reading direction and keybindings. No MComix source code, artwork, or binaries are included, and Comet is not an official MComix port or affiliated project. See `NOTICE.md`.

## License

MIT. See `LICENSE`.
