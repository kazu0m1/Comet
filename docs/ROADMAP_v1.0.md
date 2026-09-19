# Comet v1.0 roadmap

> Baseline: v0.3.0 released and Windows-validated on 2026-09-19.

## Goal

Promote the proven v0.3.0 reader baseline to the first general-availability Comet release without adding another broad feature subsystem.

The v1.0 scope is frozen at the original FR-001..FR-025 and NFR-001..NFR-010 requirements.

## Release strategy

1. **Scope freeze**
   - No new reader subsystem before v1.0 GA.
   - PDF, library/database UI, slideshow, magnifier, image enhancement/editing, and large architectural rewrites remain deferred.
   - WebP, RAR/CBR, and 7z/CB7 are retained because they are already implemented and validated.

2. **RC1 packaging**
   - Use semantic version 1.0.0-rc1.
   - CI restores, builds, runs smoke tests, publishes Windows x64, compiles the installer, and uploads the RC1 artifact.

3. **RC1 acceptance**
   - Reuse automated evidence from the v0.3.0 regression matrix.
   - Run one compact Windows pass focused on rendering/input, session/restart behavior, installer, and associations.
   - Do not re-benchmark performance unless normal reading feels slower than the v0.2.0 baseline.

4. **GA decision**
   - If RC1 exposes no release-blocking defect requiring code or packaging changes, prepare v1.0.0 directly.
   - Create RC2 only if RC1 requires a material fix.

## RC1 hands-on gate

1. Install and launch.
2. Check page turns, single/double page, cover alone, manga/RTL, Best Fit, Fit Width, Fit Height, Manual 100%, thumbnails, fullscreen, status bar, and Smart Scroll.
3. Confirm temporary zoom survives an adjacent archive in-session and resets after restart.
4. Confirm reading position restores.
5. Resize window/sidebar and toggle fullscreen; confirm fit remains correct.
6. Open one CBR and one CB7.
7. Confirm no obvious untranslated reader UI.
8. Uninstall and confirm file associations/defaults remain healthy.

Actual multi-monitor DPI movement need not be repeated if unavailable; automated DPI coverage and previous Windows validation remain accepted evidence.

## Exit criteria

- CI green for the candidate baseline.
- RC1 Windows hands-on gate passes.
- No known release blocker remains.
- Final v1.0.0 metadata and packaging agree.
- Final Release workflow produces ZIP, Setup.exe, and SHA256SUMS.txt.

## Current phase

v1.0.0-rc1 preparation is active. No feature work is planned before the RC1 Windows gate unless validation exposes a release-blocking regression.
