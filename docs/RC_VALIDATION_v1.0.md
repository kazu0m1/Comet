# Comet v1.0 RC validation

> Candidate: v1.0.0-rc1
> Baseline: v0.3.0 released and Windows-validated on 2026-09-19.

## Acceptance evidence

| Acceptance | Evidence entering RC1 | RC1 action |
|---|---|---|
| AC-001 ZIP/CBZ double-click | Installer/association validated through v0.3.0 | Short association check |
| AC-002 Best Fit | Automated fit calculations + Windows validation | Visual check |
| AC-003 Resize/DPI | Automated DPI coverage + prior Windows validation | Resize/sidebar/fullscreen; monitor move if available |
| AC-004 Manga mode | Implemented and validated | Visual/input check |
| AC-005 Temporary zoom | Automated lifecycle + Windows validation | Adjacent archive + restart |
| AC-006 Reading position | Persistence coverage + restart validation | Reopen check |
| AC-007 Page turn | Prefetch/cache + measured baseline | Subjective regression check |
| AC-008 Error resilience | Automated broken-page sequence + Windows validation | No repeat unless suspected |
| AC-009 Natural sort | Numeric + Unicode automated coverage | No repeat |
| AC-010 Localization | EN/JA parity smoke test + Windows usage | Obvious-text check |

## RC1 manual checklist

- [ ] Install Comet-v1.0.0-rc1-win-x64-Setup.exe.
- [ ] Launch and open a representative ZIP/CBZ.
- [ ] Check page turns, double page, cover alone, manga mode, fit modes, thumbnails, fullscreen, status bar, and Smart Scroll.
- [ ] Confirm temporary zoom persists to an adjacent archive.
- [ ] Restart; confirm temporary zoom resets while the persistent base mode remains.
- [ ] Confirm reading position restores.
- [ ] Resize window/sidebar and toggle fullscreen; confirm fit.
- [ ] Open one CBR and one CB7.
- [ ] Confirm no obvious untranslated reader UI.
- [ ] Uninstall and confirm associations/defaults remain healthy.

## Decision rule

- **Pass:** proceed to final v1.0.0 preparation without RC2.
- **Fail with code/packaging change:** fix and issue v1.0.0-rc2.
- **Documentation-only correction:** update records without forcing RC2 unless the distributed candidate is affected.
