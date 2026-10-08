# Comet v1.0.5 release candidate

Comet v1.0.5 is a focused fullscreen behavior maintenance release.

## Fixed

- F and F11 use a native borderless fullscreen transition that covers the taskbar without briefly restoring a maximized window to its normal size.
- Esc exits fullscreen. F and F11 still toggle it.
- Exiting fullscreen restores the prior normal/maximized native window placement, style, and topmost setting.

## Validation

- Full CI build, smoke tests, Windows x64 publish, and Setup package generation are required.
- A short Windows hands-on check is required for a consistent fullscreen transition from normal/maximized, taskbar coverage and Esc state restoration; CI cannot verify on-screen transitions.
