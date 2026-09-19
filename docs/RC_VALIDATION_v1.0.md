# Comet v1.0 RC validation

> Candidate: v1.0.0-rc1
> Baseline: v0.3.0 released and Windows-validated on 2026-09-19.

## Acceptance evidence

| Acceptance | Evidence entering RC1 | RC1 result |
|---|---|---|
| AC-001 ZIP/CBZ double-click | Installer/association validated through v0.3.0 | Passed |
| AC-002 Best Fit | Automated fit calculations + Windows validation | Passed |
| AC-003 Resize/DPI | Automated DPI coverage + prior Windows validation | Passed for available resize/sidebar/fullscreen checks |
| AC-004 Manga mode | Implemented and validated | Passed |
| AC-005 Temporary zoom | Automated lifecycle + Windows validation | Passed |
| AC-006 Reading position | Persistence coverage + restart validation | Passed |
| AC-007 Page turn | Prefetch/cache + measured baseline | Passed; no perceived regression reported |
| AC-008 Error resilience | Automated broken-page sequence + Windows validation | Accepted existing evidence |
| AC-009 Natural sort | Numeric + Unicode automated coverage | Accepted existing evidence |
| AC-010 Localization | EN/JA parity smoke test + Windows usage | Passed obvious-text check |

## RC1 manual checklist

- [x] Install Comet-v1.0.0-rc1-win-x64-Setup.exe.
- [x] Launch and open a representative ZIP/CBZ.
- [x] Check page turns, double page, cover alone, manga mode, fit modes, thumbnails, fullscreen, status bar, and Smart Scroll.
- [x] Confirm temporary zoom persists to an adjacent archive.
- [x] Restart; confirm temporary zoom resets while the persistent base mode remains.
- [x] Confirm reading position restores.
- [x] Resize window/sidebar and toggle fullscreen; confirm fit.
- [x] Open one CBR and one CB7.
- [x] Confirm no obvious untranslated reader UI.
- [x] Uninstall and confirm associations/defaults remain healthy.

## Result

**RC1 PASSED on 2026-09-19.** No release-blocking defect was found and no code or packaging change was required as a result of hands-on validation. RC2 was not required. The final `v1.0.0` Release workflow subsequently completed successfully and the GA release was published on 2026-09-19.
