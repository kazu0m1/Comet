# Comet v0.1.0

The first functional Comet preview focuses on the reading path that motivated the
project: open a ZIP quickly, read in right-to-left manga mode with Best Fit, and
adjust the displayed size immediately with Ctrl+mouse-wheel when needed.

Highlights:
- ZIP/CBZ direct reading; no pre-extraction step.
- Best Fit, Fit Width, Fit Height, Manual 100%, and temporary zoom.
- Double-page reading with the cover shown alone and right-to-left page placement.
- MComix-style keyboard controls for the implemented reader functions.
- Smart scroll, automatic rollover into the next/previous ZIP or CBZ, reading-position memory,
  bookmarks, fullscreen, and drag-and-drop.
- Compact MComix-style thumbnail sidebar with prioritized asynchronous loading.
- Arrow-key navigation and left/right click zones for forward/back navigation.
- DPI-correct Manual 100% mode plus image size and actual zoom in the status bar.
- Self-contained Windows x64 portable package and Inno Setup installer produced by
  the release workflow.

This is a pre-1.0 release. WebP, RAR/CBR, 7z/CB7, and PDF are outside this milestone.
The v0.1.0 release candidate passed Windows 11 installer and uninstall validation,
including ZIP/CBZ handler registration and confirmation that ZIP handling remains intact
after uninstall.
