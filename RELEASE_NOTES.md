# Comet v1.0.5

Comet v1.0.5 is a focused fullscreen behavior maintenance release.

## Fixed

- F and F11 use a native borderless fullscreen transition that covers the taskbar without briefly restoring a maximized window to its normal size.
- Esc exits fullscreen. F and F11 still toggle it.
- Exiting fullscreen restores the prior normal/maximized native window placement, style, and topmost setting.

## Validation

- CI #115 passed: build, smoke tests, Windows x64 self-contained publish, and Setup/portable ZIP packaging.
- Windows hands-on gate passed on 2026-10-08: both normal and maximized transitions are smooth, the taskbar is covered, and Esc restores the previous normal/maximized state.
