# Comet v1.0.6

Comet v1.0.6 fixes thumbnail sidebar positioning after returning from fullscreen or another hidden-sidebar mode.

## Fixed

- After advancing pages in fullscreen, returning with Esc or F/F11 shows the thumbnail corresponding to the currently displayed page instead of leaving the sidebar at the original scroll position.
- The same behavior applies when restoring the sidebar with I or toggling thumbnail visibility.
- Thumbnail selection is maintained while hidden. Scroll synchronization runs only when the sidebar becomes visible, after its layout is ready.
- Existing thumbnail click navigation, independent thumbnail-wheel scrolling, and fullscreen transitions are unchanged.

## Validation

- CI #119 succeeded: Build, smoke tests, Windows x64 publish, installer/portable ZIP packaging, and release-candidate artifact upload.
- This isolated visibility synchronization fix does not require a separate Windows hands-on gate.
