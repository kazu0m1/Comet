# Comet v1.0.0 RC1

Comet v1.0.0 RC1 is the release candidate for the first general-availability Comet release.
It adds no broad reader subsystem beyond the Windows-validated v0.3.0 baseline; its purpose is final validation of the frozen v1.0 requirements and packaging.

Candidate baseline:
- FR-001 through FR-025 and NFR-001 through NFR-010 are implemented with no missing core requirement identified.
- ZIP/CBZ and image-folder reading, reader modes, fit/zoom, Smart Scroll, adjacent archives, thumbnails, fullscreen, persistence, bookmarks, settings, drag-and-drop, damaged-page continuation, and Japanese/English UI are retained.
- WebP, RAR/CBR, and 7z/CB7 remain supported beyond the original baseline.
- The v0.2.0 performance/cache path and v0.3.0 state/localization/packaging hardening remain locked.
- Windows x64 portable ZIP and Inno Setup installer remain the release outputs.

PDF, library/database UI, slideshow, magnifier, and image editing/enhancement remain outside v1.0.
If RC1 passes without a release-blocking defect requiring code or packaging changes, the next step is final v1.0.0 rather than RC2.
