# Comet v0.2.0 performance baseline

Windows hands-on field measurement for the JPEG tuning phase.

> This is a field measurement, not a hardware-normalized benchmark. The same class of
> manga ZIP was used for before/after comparison, with typical JPEG pages around
> 1619x2048 pixels and roughly 0.5-1 MiB each.

## Before JPEG tuning

| Operation | Count | Average ms | P50 ms | P95 ms | Max ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| open.first-render | 2 | 36.24 | 29.82 | 42.66 | 42.66 |
| open.source | 2 | 9.14 | 5.36 | 12.91 | 12.91 |
| page.decode | 319 | 38.10 | 47.73 | 72.39 | 143.76 |
| page.read | 319 | 4.09 | 2.59 | 15.27 | 51.06 |
| render.page | 94 | 23.44 | 1.90 | 114.12 | 127.97 |

Aggregate cache hit rate: **36.5%** (236 hits / 411 misses).

## After JPEG tuning

| Operation | Count | Average ms | P50 ms | P95 ms | Max ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| open.first-render | 1 | 35.24 | 35.24 | 35.24 | 35.24 |
| open.source | 1 | 5.06 | 5.06 | 5.06 | 5.06 |
| page.bitmap-decode | 964 | 10.26 | 10.53 | 13.86 | 29.25 |
| page.decode | 964 | 10.72 | 10.98 | 14.42 | 30.97 |
| page.probe | 792 | 0.01 | 0.00 | 0.01 | 1.22 |
| page.read | 964 | 2.80 | 2.33 | 5.13 | 32.64 |
| render.page | 355 | 4.10 | 2.04 | 15.77 | 74.21 |

Aggregate cache hit rate: **51.9%** (1286 hits / 1190 misses).

## Result

- End-to-end page decode average improved from **38.10 ms to 10.72 ms** (about **72% lower**).
- Render-page P95 improved from **114.12 ms to 15.77 ms** (about **86% lower**).
- Render-page median remains effectively instant at **2.04 ms**, preserving the fast cached-page behavior.
- Source-dimension probing is now effectively free at **0.01 ms average**.
- Archive I/O remains a small part of the cold path at roughly **2.8 ms average**.
- The remaining bitmap decode cost is about **10 ms average**, which is acceptable for the v0.2.0 target.

## Tuning changes retained

- JPEG/PNG/GIF/BMP source dimensions are read from image headers when possible instead of forcing WIC `OnLoad`.
- Array-backed page bytes are passed to WIC without an extra full byte-array copy.
- WIC no longer receives a decode width larger than the source image; visual upscaling is left to WPF rendering.
- The cache/prefetch experiments that worsened hands-on responsiveness were reverted to the faster CI #45 behavior.

## Decision

The JPEG tuning phase is complete. Further decoder replacement (for example, moving JPEG
from WIC to SkiaSharp) is not justified before v0.2.0 RC unless a reproducible regression
appears.
