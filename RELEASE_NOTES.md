# Comet v1.0.1

Comet v1.0.1 is a small reader-interaction update built on the v1.0.0 general-availability baseline.

## Added

- When the displayed page or spread is larger than the image viewport, hold the left mouse button and drag to pan it horizontally or vertically.
- Panning is clamped to the rendered content, so the image cannot be dragged beyond its available scroll range.
- The existing left-click page navigation remains intact: movement must exceed the Windows drag-distance threshold before the gesture becomes a pan.
- When the content already fits inside the viewport, dragging does not enter pan mode.

## Validation

- Automated smoke coverage verifies click-versus-drag thresholding, non-pannable content, drag deltas, and suppression of click navigation after an actual drag.
- Build, smoke tests, Windows x64 self-contained publish, and installer packaging passed in CI.
- No separate Windows hands-on gate was required because the change is isolated to pointer gesture classification and the existing bounded viewport scrolling path.
