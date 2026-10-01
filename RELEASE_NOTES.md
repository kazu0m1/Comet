# Comet v1.0.4

Comet v1.0.4 adds recursive ZIP/CBZ reading while preserving the existing reader UX.

## Added

- Opening an outer ZIP/CBZ now discovers image pages and contained ZIP/CBZ archives in natural filename order.
- Contained ZIP/CBZ archives are recursively traversed and flattened into one continuous page sequence, so an outer `A.zip` containing `B.zip`, `C.cbz`, and deeper ZIP/CBZ files can be read as one book.
- Page names retain their archive path, for example `B.zip/page1.jpg` and `C.cbz/D.zip/page1.jpg`.
- The outer archive remains the book identity/path, so reading position and bookmarks continue to belong to the outer archive.

## Safety and resource handling

- Nested archive contents are expanded to a temporary Comet workspace instead of being retained wholly in memory.
- Temporary nested files are deleted when the outer book source is closed.
- Recursion is limited to 16 nested ZIP/CBZ levels.
- A single nested archive is limited to 8 GiB expanded size, with a 32 GiB total temporary-storage safety limit per opened outer archive.
- RAR/CBR and 7z/CB7 continue to work as top-level formats but are not recursively traversed in v1.0.4.

## Validation

- Added an automated fixture that creates an outer ZIP containing a direct image, `B.zip`, `C.cbz`, and a deeper `D.zip`.
- Smoke coverage verifies recursive flattening, natural order, page bytes, ZIP/CBZ handling at multiple levels, factory routing, and preservation of the outer book path.
- Build and recursive-archive smoke tests pass without requiring a separate UI hands-on gate because the reader UI and navigation paths are unchanged.
