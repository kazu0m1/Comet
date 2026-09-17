# Contributing

Comet is currently a small Windows-focused project. Contributions should keep the
reader responsive and avoid adding startup-time dependencies without a measured
benefit.

## Development rules

1. Keep ZIP I/O and image decoding off the UI thread.
2. Fit calculations must use the actual image viewport, not outer window size.
3. Do not copy source code or artwork from MComix; behavior compatibility is the
   reference, not code reuse.
4. Add or update smoke tests for Core/Infrastructure behavior.
5. Run `./scripts/verify.ps1` on Windows before opening a pull request.

## Pull requests

Explain the user-visible effect, performance impact, and how the change was
verified. Screenshots are useful for UI changes but are not required for logic-only
changes.
