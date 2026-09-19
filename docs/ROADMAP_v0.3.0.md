# Comet v0.3.0 roadmap

> Baseline: v0.2.0 released and Windows-validated on 2026-09-19.

## Goal

Turn the feature-complete v1.0 requirements baseline into a polished, regression-resistant
Windows reader without adding another major subsystem.

v0.3.0 is intentionally a **completion and hardening milestone**, not a format-expansion milestone.

## Priority order

1. **Formal v1.0 requirement audit — complete**
   - FR-001 through FR-025 and NFR-001 through NFR-010 are mapped in `IMPLEMENTATION_STATUS_v1.0.md`.
   - No missing core v1.0 feature has been identified.

2. **Reader regression matrix — complete in code**
   - Added `REGRESSION_MATRIX_v0.3.0.md`.
   - Folder/individual-image routing and all declared archive/image extensions now have deterministic smoke coverage.
   - Deterministic paths are covered; remaining checks are limited to the compact RC1 Windows rendering/input/integration gate.

3. **UI and localization audit — complete in code**
   - Bookmark action text and open-failure messaging are localized.
   - Toolbar tooltips are normalized through the localization table.
   - English/Japanese localization tables are now parity-checked in smoke tests.
   - Remaining symbols, file names, units, and technical diagnostics are intentionally language-neutral or diagnostic content.

4. **Settings and state hardening — complete in code**
   - Corrupt/missing settings fallback is covered.
   - Loaded settings are normalized for invalid enum values and unsafe numeric ranges.
   - Interrupted settings/reading-state writes clean up temporary files on a best-effort basis.
   - Reading-position/source replacement behavior remains covered and Windows-validated.

5. **Performance and memory guardrails — baseline locked**
   - Keep the v0.2.0 JPEG performance baseline as the regression target.
   - Do not retune prefetch/cache unless a reproducible regression appears.
   - Weighted LRU behavior remains smoke-tested; RC2 memory figures remain observation data, not a hard promise.

6. **Windows packaging polish — complete in code**
   - CI development artifacts now use `0.3.0-dev` naming.
   - Release workflow remains tag-driven.
   - GitHub Desktop is the documented repository/tagging workflow.
   - Publish/package/installer fallback versions now agree on `0.3.0-dev`.
   - Final candidate still requires the short Windows install/uninstall and association gate.

## Explicit non-goals

- PDF support.
- Library/database UI.
- Slideshow.
- Magnifier.
- Image enhancement/editing.
- Replacing WPF rendering.
- Large architectural rewrites.

## Acceptance target

v0.3.0 is complete when:

- Every original v1.0 requirement has an explicit current implementation/validation status.
- No known release-blocking regression remains in the core reading workflow.
- Automated coverage protects the highest-risk behavior added through v0.2.0.
- Remaining hands-on validation is short and repeatable.
- The Windows installer and portable package remain cleanly releasable.
- Performance remains in the v0.2.0 class on the established JPEG workload.

## Exit toward v1.0

If the final compact Windows gate passes without discovering a missing core requirement, the next
milestone should be a v1.0 release-candidate cycle rather than another broad feature-expansion release.


## Current phase

RC1 packaging is active. No further code-level feature work is planned before the compact
Windows hands-on gate unless CI exposes a regression.
