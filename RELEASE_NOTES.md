# Comet v1.0.5 release candidate

Comet v1.0.5 is a focused fullscreen behavior maintenance release.

## Fixed

- F and F11 now enter true fullscreen even when Comet is already maximized, covering the Windows taskbar.
- Esc exits fullscreen. F and F11 still toggle it.
- Exiting fullscreen restores the prior normal/maximized window state and topmost/window-chrome settings.

## Validation

- Full CI build, smoke tests, Windows x64 publish, and Setup package generation are required.
- A short Windows hands-on check is required for taskbar coverage and Esc state restoration, since a CI build alone cannot verify taskbar z-order.
