# Comet v1.1.0

Comet v1.1.0 adds native PDF reading while preserving the existing comic-archive viewer.

## Added

- Open PDF documents with an isolated, non-V8 PDFium worker. No PDF JavaScript execution, OCR processing, or modifications to source documents.
- Read PDFs as single pages, facing spreads or virtualized continuous scroll, with per-document reading direction (left-to-right/right-to-left), cover-alone preference, display fit and reading position.
- Fixed Size / Fit to Size: absolute physical screen-pixel width or height, default height **1,400 px**.
- Select and copy existing PDF text including selections spanning pages; Ctrl+F search and highlight matches. Text-layer OCR scans work without re-running OCR.
- Navigate embedded outlines and links, page numbers and PDF page labels; inspect metadata; confirm before opening external HTTP(S) links.
- Open password-protected PDFs, rotate individual pages or the document, and optionally crop white margins from the rendered image.
- Mixed PDF / ZIP / CBZ / RAR / CBR / 7z / CB7 adjacent-file navigation. Optional PDF associations in Windows Setup.

## Improved

- Continuous reading: edge-to-edge pages on a consistent `#282828` background, dragging blank areas to scroll, text-first drag selection, Esc selection clearing, and viewport-synchronized status and thumbnails.
- After thumbnail jumps, warm the selected page and its neighbors; re-request missing images when WPF reuses a virtualized row.
- Scale tall/oversized PDF pages within rendering limits instead of rejecting them.
- During initial continuous PDF opening, prioritize only needed visible pages instead of prefetching a three-page neighborhood.

## Limitations

- Comet **reads existing text layers only**. An image-only scanned PDF can be viewed, but cannot be searched or selected without pre-existing OCR text.
- Comet does not edit PDF files, add OCR, create annotations or signatures.

## Validation and distribution

- Repeated Windows 11 x64 hands-on testing confirmed fixes for the nine reported PDF/continuous-view defects, including manga mode, high-zoom navigation and spread reloading.
- A small subjective opening-time difference remained; the user accepted the current responsiveness. No quantified speed improvement is claimed.
- The GitHub CI builds/tests the solution, exercises PDFium native open/render/text/outline operations, and produces Setup and portable Windows packages.
- The PDF worker bundle includes the upstream PDFium license and all third-party license files in `pdf-worker/licenses/`. Packaging verifies the exact upstream archive SHA-256 and checks that `pdfium.dll` matches its pinned origin.
