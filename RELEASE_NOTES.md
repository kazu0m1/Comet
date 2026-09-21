# Comet v1.0.2 candidate

Comet v1.0.2 is a small interaction and navigation maintenance update.

## Changed

- Mouse-wheel input over the thumbnail sidebar scrolls only the thumbnail list and no longer triggers Smart Scroll or page/archive navigation in the main reader.
- Clicking a thumbnail continues to jump directly to that page.
- Mouse-wheel behavior over the main reader remains unchanged.

## Added

- Right-click the main reader to open a localized context menu.
- The context menu exposes page/archive navigation, fit modes, double-page mode, manga mode, stretch, thumbnail visibility, bookmark creation, and fullscreen.
- Checkable context-menu items reflect the current reader settings.

## Validation

- Build and smoke tests pass with the new XAML and input routing.
- Before tagging v1.0.2, verify thumbnail wheel isolation, thumbnail click navigation, and viewer right-click context-menu behavior on Windows.
