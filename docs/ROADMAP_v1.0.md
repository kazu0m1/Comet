# Comet v1.0 roadmap

> Baseline: v0.3.0 released and Windows-validated on 2026-09-19.

## Goal

Promote the proven v0.3.0 reader baseline to the first general-availability Comet release without adding another broad feature subsystem.

The v1.0 scope is frozen at the original FR-001..FR-025 and NFR-001..NFR-010 requirements.

## Release strategy

1. Scope freeze: complete; no new reader subsystem before v1.0 GA.
2. RC1 packaging: complete; CI, portable build, installer, and RC artifact passed.
3. RC1 acceptance: complete; Windows hands-on gate passed on 2026-09-19.
4. GA preparation: active; final metadata and packaging use version 1.0.0.

PDF, library/database UI, slideshow, magnifier, image enhancement/editing, and large architectural rewrites remain deferred. WebP, RAR/CBR, and 7z/CB7 remain supported because they are already implemented and validated.

## Exit criteria

- [x] v1.0 requirements scope frozen.
- [x] RC1 CI green.
- [x] RC1 Windows hands-on gate passed.
- [x] No known release blocker remains.
- [x] Final v1.0.0 metadata and packaging prepared.
- [ ] Final v1.0.0 Release workflow produces ZIP, Setup.exe, and SHA256SUMS.txt.

## Current phase

RC1 passed without a release-blocking defect. RC2 is skipped. v1.0.0 final release preparation is active; after the final CI passes, the remaining operation is the v1.0.0 tag and Release workflow.
