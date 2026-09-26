# Comet v1.0.3 candidate

Comet v1.0.3 is a small navigation-consistency maintenance update.

## Changed

- Arrow-key navigation now crosses comic-archive boundaries when the current book has no further page in that direction.
- Down continues to the next archive and Up can return to the previous archive.
- Left/Right preserve the active reading direction: in manga mode Left advances and Right goes back; in left-to-right mode the directions are reversed.
- Mouse-wheel and arrow-key archive-boundary navigation now share the same page-or-archive decision path.

## Validation

- Build and smoke tests pass after the navigation refactor.
- Before tagging v1.0.3, verify on Windows that an arrow key can move from the last page into the next archive and that ordinary in-book arrow navigation remains unchanged.
