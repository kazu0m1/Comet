# Comet v1.0.3

Comet v1.0.3 is a small navigation-consistency maintenance update.

## Changed

- Arrow-key navigation now crosses comic-archive boundaries when the current book has no further page in that direction.
- Down continues to the next archive and Up can return to the previous archive.
- Left/Right preserve the active reading direction: in manga mode Left advances and Right goes back; in left-to-right mode the directions are reversed.
- Mouse-wheel and arrow-key archive-boundary navigation now share the same page-or-archive decision path.
- Arrow-key input is consumed before asynchronous archive loading so Up/Down do not transfer keyboard focus to the menu bar during a book transition.

## Validation

- Build, smoke tests, Windows x64 self-contained publish, and installer packaging passed in CI.
- Windows hands-on validation passed for forward/back archive rollover by arrow key, unchanged in-book arrow behavior, and retention of reader focus after Up/Down archive transitions.
