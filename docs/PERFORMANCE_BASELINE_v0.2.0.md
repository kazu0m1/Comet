# Comet v0.2.0 performance baseline

First Windows hands-on measurement after introducing opt-in performance tracing.

> This is a field measurement, not a hardware-normalized benchmark. The original trace
> combined full-page and thumbnail cache/decode events, so cache/decode totals below are
> intentionally treated as an aggregate baseline.

| Operation | Count | Average ms | P50 ms | P95 ms | Max ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| open.first-render | 2 | 36.24 | 29.82 | 42.66 | 42.66 |
| open.source | 2 | 9.14 | 5.36 | 12.91 | 12.91 |
| page.decode | 319 | 38.10 | 47.73 | 72.39 | 143.76 |
| page.read | 319 | 4.09 | 2.59 | 15.27 | 51.06 |
| render.page | 94 | 23.44 | 1.90 | 114.12 | 127.97 |

Aggregate cache hit rate: **36.5%** (236 hits / 411 misses).

## Interpretation

- Cold opening is already fast: first rendered page averages about 36 ms.
- Normal page turns are usually effectively instant: render-page P50 is 1.9 ms.
- Tail latency is dominated by cold work: render-page P95 is about 114 ms.
- Archive read time is relatively small; image decode is the larger cold-path cost.
- The aggregate cache rate cannot distinguish full-page behavior from thumbnail activity.

## Follow-up instrumentation

Subsequent builds split performance operations into `page.*` and `thumbnail.*`
families and cancel/debounce stale thumbnail neighborhood warmups. Repeat the same
reading session after that change before considering more aggressive decode or prefetch
changes.
