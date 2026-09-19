# Comet v0.3.0 regression matrix

> Purpose: keep hands-on testing short by automating deterministic behavior and reserving
> Windows validation for rendering, integration, and installer behavior.

| Area | Automated coverage | Windows hands-on status | v0.3.0 action |
|---|---|---|---|
| ZIP/CBZ | Filtering, natural order, page bytes, factory route | Passed in v0.2.0 | Keep |
| RAR/CBR | Real RAR fixture + solid CBR decode | Passed in v0.2.0 | Keep |
| 7z/CB7 | Generated CB7 + solid CB7 decode | Passed in v0.2.0 | Keep |
| Image folder | Unicode natural order, filtering, read bytes, factory route | Existing workflow used | Added automated coverage |
| Individual image | Factory routes through containing folder | Existing workflow used | Added automated coverage |
| Image formats | All declared extensions recognized; JPEG/WebP decode paths exercised | JPEG/WebP passed | Keep |
| Natural sort | Numeric + Unicode | Passed | Keep |
| Fit modes | Best Fit/stretch + DPI Manual 100% calculations | Passed | Keep |
| Single/double page | Spread index planning | Passed | Short visual check only |
| Cover alone | Spread planner | Passed | Short visual check only |
| Landscape alone | Main reader behavior | Passed previously | Short visual check only |
| Manga/LTR | Core setting/navigation/render placement | Passed | Visual/input check remains |
| Temporary zoom | Session survives adjacent archive, resets for new process | Passed | Keep |
| Smart Scroll | Reader integration | Passed | Hands-on only |
| Adjacent archives | All supported archive extensions + natural order | Passed | Expanded automated coverage |
| Thumbnail sidebar | Cache/UI integration | Passed | Hands-on only |
| Reading position | JSON round-trip + source fingerprint | Passed restart test | Keep |
| Bookmarks | JSON round-trip + dialog flow | Basic behavior passed | Keep |
| Damaged page | Valid → broken → valid decode continuation | Passed | Keep |
| Settings | Round-trip, migration, corrupt fallback, invalid-value normalization | Existing behavior passed | Expanded automated coverage |
| DPI change | Scale calculation automated | Passed | Hands-on only for actual monitor move |
| JPEG performance | Instrumented | v0.2.0 baseline recorded | Do not retune without regression |
| Memory pressure | Weighted LRU behavior automated | RC2 observed ~200 MiB idle/~600 MiB peak | Observation, not hard threshold |
| Installer/associations | CI builds installer | Passed v0.2.0 | Final hands-on gate |
| Uninstall | Not practical in smoke test | Passed v0.2.0 | Final hands-on gate |
| Japanese/English UI | Localization table/code audit | Japanese used throughout development | Final short language check if needed |

## Final hands-on gate target

If automated CI remains green, v0.3.0 should require only a compact Windows pass:

1. Open one representative ZIP and confirm page turns, double page, manga mode, fit modes,
   thumbnails, fullscreen, status bar, and temporary zoom.
2. Open one CBR and one CB7 fixture.
3. Restart and confirm reading-position restoration.
4. Install/uninstall the candidate and confirm optional associations do not damage existing defaults.
5. Confirm no obvious untranslated user-facing text in the active Windows language.

No broad performance benchmark is required unless normal reading feels slower than v0.2.0.
