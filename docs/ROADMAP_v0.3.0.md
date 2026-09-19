# Comet v0.3.0 roadmap

> Baseline: v0.2.0 released and Windows-validated on 2026-09-19.

## Goal

Turn the feature-complete v1.0 requirements baseline into a polished, regression-resistant
Windows reader without adding another major subsystem.

v0.3.0 is intentionally a **completion and hardening milestone**, not a format-expansion milestone.

## Why this phase

By v0.2.0, Comet already implements essentially all Must/Should requirements in the original
v1.0 baseline, and also goes beyond it with WebP, RAR/CBR, and 7z/CB7 support.

The remaining risk is not missing headline features. It is accumulated edge cases, UI roughness,
settings/state behavior, packaging drift, and regressions across the many interacting reader paths.

## Priority order

1. **Formal v1.0 requirement audit**
   - Re-check FR-001 through FR-025 against the current source tree.
   - Re-check NFR-001 through NFR-010.
   - Mark each item as implemented, validated, or requiring follow-up.
   - Replace the stale v0.1.0 implementation-status document with a current matrix.

2. **Reader regression matrix**
   - Cover ZIP/CBZ/RAR/CBR/7z/CB7 and folder reading.
   - Cover single/double page, cover-alone behavior, landscape-alone behavior, manga/LTR,
     Best Fit/Fit Width/Fit Height/Manual 100%, temporary zoom, fullscreen, thumbnails,
     adjacent archive navigation, reading-state restore, bookmarks, and damaged pages.
   - Prefer automated smoke coverage where behavior is deterministic.
   - Keep a short Windows hands-on checklist only for behavior that actually needs a person.

3. **UI and localization audit**
   - Search the WPF UI and dialogs for remaining hard-coded user-visible English/Japanese strings.
   - Normalize labels, tooltips, status text, dialogs, and menu terminology.
   - Preserve the MComix-inspired reading workflow and the v0.2.0 status-bar layout.
   - Avoid visual redesign for its own sake.

4. **Settings and state hardening — in progress**
   - Corrupt/missing settings fallback is covered.
   - Loaded settings are normalized for invalid enum values and unsafe numeric ranges.
   - Interrupted settings/reading-state writes clean up temporary files on a best-effort basis.
   - Verify window/UI-state persistence remains sane across upgrades.
   - Verify reading positions/bookmarks survive normal upgrades and reject replaced books as intended.
   - Keep temporary zoom session-only.

5. **Performance and memory guardrails**
   - Keep the v0.2.0 JPEG performance baseline as a regression target.
   - Do not retune prefetch/cache unless a reproducible regression appears.
   - Add targeted checks around stale-load cancellation and bounded bitmap caching where practical.
   - Treat the RC2 memory figures as an observation baseline, not a promise.

6. **Windows packaging polish**
   - Re-check application/file-association icon behavior.
   - Re-check installer/uninstaller and optional archive associations.
   - Ensure release notes, README, version labels, and package names agree.
   - Keep GitHub Desktop as the documented release workflow for repository operations.

## Explicit non-goals

The following remain outside v0.3.0 unless a blocking requirement emerges:

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

If this milestone closes without discovering a missing core requirement, the next milestone should
be a v1.0 release-candidate cycle rather than another broad feature-expansion release.
