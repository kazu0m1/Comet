# Release checklist

## Historical validation

### v0.1.0

- [x] GitHub Actions restore/build/smoke-test/self-contained publish.
- [x] Core ZIP/CBZ reading, fit modes, manga mode, fullscreen, thumbnails, navigation, Manual 100%, fast shutdown, adjacent archive rollover.
- [x] Inno Setup install, optional association registration, uninstall, and intact ZIP handling.

### v0.2.0

- [x] WebP, RAR/CBR, 7z/CB7.
- [x] Damaged-page continuation and reading-position restart restore.
- [x] DPI refresh and Manual 100% regression coverage.
- [x] MComix-style status details.
- [x] JPEG decode tuning: measured page.decode average 38.10 ms -> 10.72 ms.
- [x] Measured render.page P95 114.12 ms -> 15.77 ms.
- [x] RC2 memory-pressure observation: about 200 MiB idle and 600 MiB peak during rapid paging on the measured workload, without perceived speed regression.
- [x] Installer/associations/uninstall and release assets validated.
- [x] v0.2.0 released successfully.

## v0.3.0 development gate

- [x] Formal FR-001..FR-025 / NFR-001..NFR-010 implementation audit created.
- [x] Regression matrix created.
- [x] Persisted settings normalize corrupt-but-valid enum/numeric values.
- [x] Settings and reading-state temp writes clean up on interrupted writes.
- [x] Folder and individual-image factory paths covered by smoke tests.
- [x] All declared image/archive extensions covered by routing smoke tests.
- [x] CI development artifacts use 0.3.0-dev naming.
- [x] Bookmark action/open-failure localization cleanup.
- [x] Thumbnail sidebar width is persisted after splitter resize.
- [x] Final v0.3.0 Windows hands-on gate.
- [x] Final installer/uninstaller regression.
- [x] Final candidate package integrity check.
- [x] `v0.3.0` tag pushed and GitHub Release workflow completed successfully.
- [x] v0.3.0 release assets published: portable ZIP, Setup.exe, and SHA256SUMS.txt.

## Final hands-on gate

RC1 passed the Windows 11 hands-on gate. Installation, launch, uninstall, and file-association cleanup were confirmed without issues.

The intended final manual pass is deliberately short:

1. Open one representative ZIP and check page turns, double page, manga mode, Best/Fit Width/Fit Height/Manual 100%, thumbnails, fullscreen, status bar, and temporary zoom.
2. Resize the thumbnail sidebar, restart Comet, and confirm the width is restored.
3. Open one CBR and one CB7 fixture.
4. Restart on a middle page and confirm reading position restoration.
5. Install/uninstall the candidate and confirm optional archive associations do not damage existing defaults.
6. Confirm there is no obvious untranslated user-facing text in the active Windows language.

A fresh performance benchmark was not required because normal reading showed no reported regression from the v0.2.0 baseline.

**v0.3.0 release gate: PASSED on 2026-09-19.**

## Tag and release with GitHub Desktop

Repository operations for Comet are performed with **GitHub Desktop**, not Git CLI.

After the final gate is complete:

1. In GitHub Desktop, **Fetch origin** and **Pull origin** if shown.
2. Open **History** and select the final release-preparation commit.
3. Right-click it and choose **Create Tag...**.
4. Enter the requested tag exactly, for example `v0.3.0`.
5. Choose **Create Tag**, then **Push origin** if GitHub Desktop shows it.
6. Confirm the GitHub **Release** Actions workflow finishes green.

The Release workflow creates:
- `Comet-v<version>-win-x64.zip`
- `Comet-v<version>-win-x64-Setup.exe`
- `SHA256SUMS.txt`

Pre-1.0 versions are published as GitHub prereleases.

## v1.0.0 general-availability gate

- [x] v1.0 scope frozen at FR-001..FR-025 / NFR-001..NFR-010.
- [x] v1.0.0-rc1 CI and package generation completed successfully.
- [x] v1.0.0-rc1 Windows hands-on gate passed.
- [x] RC2 skipped because no release-blocking defect was found.
- [x] Final v1.0.0 CI completed successfully.
- [x] `v1.0.0` tag pushed to the final release-gate commit.
- [x] GitHub Release workflow completed successfully.
- [x] `Comet-v1.0.0-win-x64.zip`, `Comet-v1.0.0-win-x64-Setup.exe`, and `SHA256SUMS.txt` published.
- [x] GitHub Release published as a normal release (`prerelease: false`).

**v1.0.0 milestone: CLOSED on 2026-09-19.**

## v1.0.1 maintenance release

- [x] Added left-button drag panning for oversized pages/spreads.
- [x] Preserved existing left-click page navigation through Windows drag-distance thresholding.
- [x] Added smoke coverage for click-versus-drag behavior and non-pannable content.
- [x] Implementation CI completed successfully without a separate hands-on gate.
- [x] Final v1.0.1 CI completed successfully.
- [x] `v1.0.1` tag pushed to `Prepare v1.0.1 release`.
- [x] GitHub Release workflow completed successfully.
- [x] `Comet-v1.0.1-win-x64.zip`, `Comet-v1.0.1-win-x64-Setup.exe`, and `SHA256SUMS.txt` published.
- [x] GitHub Release published as a normal release (`prerelease: false`).

**v1.0.1 maintenance release: CLOSED on 2026-09-20.**

## v1.0.2 interaction maintenance gate

- [x] Route wheel input over the thumbnail sidebar to the thumbnail ScrollViewer only.
- [x] Preserve thumbnail click-to-page navigation.
- [x] Preserve main-reader wheel Smart Scroll/page navigation.
- [x] Add localized right-click context menu to the main reader.
- [x] Implementation build and smoke tests passed.
- [x] Windows check: thumbnail wheel scrolls thumbnails without changing the main page.
- [x] Windows check: clicking a thumbnail jumps to that page.
- [x] Windows check: main-reader wheel navigation remains unchanged.
- [x] Windows check: right-click opens the context menu and a representative action works.
- [x] Final v1.0.2 CI succeeds.
- [x] `v1.0.2` tag and Release workflow succeed.

**v1.0.2 hands-on gate: PASSED on 2026-09-21.**

- [x] `Comet-v1.0.2-win-x64.zip`, `Comet-v1.0.2-win-x64-Setup.exe`, and `SHA256SUMS.txt` published.
- [x] GitHub Release published as a normal release (`prerelease: false`).

**v1.0.2 maintenance release: CLOSED on 2026-09-21.**
