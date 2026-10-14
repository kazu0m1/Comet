# Comet v1.0.7

Comet v1.0.7 changes the default startup window state.

## Changed

- Start maximized as a normal Windows window, preserving the title bar, menus, toolbar, thumbnail sidebar, status bar, and taskbar.
- F/F11 still enters the separate borderless fullscreen mode; Esc exits it to the previous window state.
- If the user restores the window manually, the existing normal-window size remains available.

## Validation

- The maximized startup property is applied in WPF XAML before the window is first shown. No changes were made to native fullscreen or the archive/reading pipeline.
- CI validates the solution build, smoke tests, Windows x64 publishing, and Setup/portable packaging. No separate user hands-on gate is required for this isolated startup-default change.
