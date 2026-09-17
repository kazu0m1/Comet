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
| Manual Zoom | Implemented | 100% mode plus temporary zoom controls. |
| Ctrl+mouse-wheel temporary zoom | Implemented | Persists to adjacent archives, resets after process restart. |
| Smart Scroll | Implemented (basic) | Reading-flow horizontal/vertical scrolling and flip-at-edge. Needs Windows UX tuning. |
| Previous/next archive | Implemented | Ctrl+Shift+P/N, natural archive sort. |
| Bounded prefetch cache | Implemented | 6-item LRU, nearby pages prefetched. |
| Natural sort | Implemented + smoke test | Numeric runs sorted naturally. |
| Fullscreen | Implemented | F/F11. |
| Thumbnail sidebar | **Deferred** | Next milestone; deliberately excluded from v0.1.0 startup path. |
| Reading position | Implemented | Per source JSON in LocalAppData. |
| Bookmark | Implemented | Add and open bookmark list. |
| Settings | Implemented | Base fit, reading direction, layout and stretch settings. |
| Japanese/English UI | Implemented | Selected from Windows UI culture. |
| MComix-compatible controls | Implemented for included features | See `KEYBINDINGS.md`. |
| Drag and drop | Implemented | Archive, folder, supported image. |
| Damaged page resilience | Implemented path | Failed page shows an error; navigation remains available. Requires Windows validation. |

## Build-validation state

Static checks completed in the authoring environment:
- XML/XAML well-formedness.
- GitHub workflow YAML parsing.
- XAML event-handler ↔ code-behind matching.
- C# structural brace checks.
- Source archive integrity.

Not possible in the authoring environment because no .NET SDK / Windows Desktop build chain is installed:
- `dotnet build` of WPF projects.
- Inno Setup compilation.
- Windows runtime/UX testing.

Those gates are encoded in `scripts/verify.ps1`, `.github/workflows/ci.yml`, and `docs/RELEASE_CHECKLIST.md`.
