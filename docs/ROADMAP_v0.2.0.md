# Comet v0.2.0 Roadmap — v1.0 Hardening

> Baseline: Comet v0.1.0 released on 2026-09-18.

## Goal

v0.2.0 is a hardening milestone. The priority is to make the existing MComix-like
reading path more reliable, measurable, and regression-resistant before expanding
to archive/document formats that were explicitly deferred beyond the v1.0 baseline.

## Scope

### 1. Regression safety

- Expand smoke/integration coverage for:
  - temporary zoom lifecycle across adjacent archives and process restart,
  - reading-position persistence,
  - settings persistence, schema migration, and corrupt-file fallback,
  - natural adjacent-archive ordering including Japanese names,
  - supported-image filtering.
- Add deterministic fixtures for damaged-image behavior.

### 2. Error resilience

- Verify FR-025 end-to-end with a deliberately broken image inside a ZIP.
- Keep navigation usable after a decode failure.
- Improve error presentation only where it does not slow the normal reading path.

### 3. DPI / resize hardening

- Validate 100%, 125%, 150%, and 200% scaling.
- Validate moving a running window between monitors with different DPI values.
- Ensure Best Fit / Fit Width / Fit Height use the actual image viewport after
  sidebar, fullscreen, and window-size changes.
- Add deferred high-resolution re-decode after resize only if visual quality requires it.

### 4. Cache / performance hardening

- Measure page-open, decode, cache-hit, and page-turn latency.
- Move decoded-page eviction from item-count-only toward a byte-aware budget if
  measurement shows large-page memory pressure.
- Keep thumbnails isolated from the foreground reading path.

### 5. Localization and UI polish

- Remove remaining hard-coded English labels in menus, toolbar, and dialogs.
- Re-check Japanese/English UI switching against FR-022.

### 6. Codec decision

- Evaluate guaranteed WebP support without regressing startup time or distribution simplicity.
- Prefer a decoder adapter that can be loaded only when needed.
- RAR/CBR, 7z/CB7, and PDF remain outside v0.2.0 unless the v1.0 hardening work is complete.

## Acceptance gates

v0.2.0 is ready when:

- expanded CI tests are green,
- reading-position and temporary-zoom behavior are regression-tested,
- damaged-image navigation is manually and automatically validated where practical,
- multi-DPI behavior is manually validated,
- no known localization gaps remain in the primary reader UI,
- performance instrumentation is sufficient to identify regressions,
- installer and clean-uninstall checks still pass.

## Non-goals

- Library/database features.
- Slideshow, magnifier, image enhancement, or external commands.
- Broad archive-format expansion before the existing v1.0 reader path is hardened.
