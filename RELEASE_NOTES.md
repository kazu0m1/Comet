# Comet v0.3.0

Comet v0.3.0 is a completion and hardening release. It does not add another major reader
subsystem; instead it audits the original v1.0 requirements, expands deterministic regression
coverage, hardens persisted state, and cleans up the remaining reader UI/localization edges.

Highlights:
- Formal FR-001 through FR-025 and NFR-001 through NFR-010 implementation audit.
- Regression matrix covering ZIP/CBZ/RAR/CBR/7z/CB7, image folders, Unicode natural
  ordering, archive routing, reading state, settings migration, and damaged pages.
- Persisted settings now normalize invalid enum/numeric values to safe ranges.
- Interrupted settings and reading-state writes clean up temporary files on a best-effort basis.
- Thumbnail-sidebar width is saved after splitter resize and restored on restart.
- Remaining bookmark/open-failure/tool-tip reader text is routed through Japanese/English localization.
- English/Japanese localization key parity is smoke-tested.
- CI and fallback package defaults are aligned on the v0.3.0 development/release-candidate line.
- GitHub Desktop is the documented repository/tag workflow.

The successful v0.2.0 JPEG performance path, cache/prefetch behavior, memory hardening,
archive support, WebP support, DPI behavior, and MComix-style status bar are deliberately
kept as the performance/UX baseline rather than retuned.

PDF support, library/database UI, slideshow, magnifier, and image editing/enhancement remain
outside this milestone.

The v0.3.0 Windows release-candidate gate passed on 2026-09-19, including install, launch,
uninstall, and file-association cleanup. No missing core requirement was exposed. The project can
therefore move toward a v1.0 release-candidate cycle rather than another broad feature-expansion
milestone.
