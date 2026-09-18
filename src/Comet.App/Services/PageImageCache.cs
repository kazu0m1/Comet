using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using Comet.Core.Abstractions;
using Comet.Core.Models;
using Comet.Core.Services;
using Comet.Platform.Windows.Imaging;

namespace Comet.App.Services;

public sealed class PageImageCache : IDisposable
{
    private readonly IBookSource _source;
    private readonly WpfBitmapDecoder _decoder;
    private readonly LruCache<(int Page, int Width), BitmapSource> _cache;
    private readonly ConcurrentDictionary<(int Page, int Width), InflightLoad> _inflight = new();
    private readonly ConcurrentDictionary<int, PixelSize> _sourcePixelSizes = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _prefetchGate = new();
    private readonly int _minimumBucketWidth;
    private readonly int _maximumBucketWidth;
    private readonly bool _captureSourcePixelSize;
    private CancellationTokenSource? _prefetchCts;

    public PageImageCache(
        IBookSource source,
        WpfBitmapDecoder decoder,
        int capacity = 6,
        int minimumBucketWidth = 512,
        int maximumBucketWidth = 4096,
        bool captureSourcePixelSize = true,
        long maxEstimatedBytes = 192L * 1024 * 1024)
    {
        _source = source;
        _decoder = decoder;
        _cache = new LruCache<(int Page, int Width), BitmapSource>(
            Math.Max(1, capacity),
            Math.Max(1, maxEstimatedBytes),
            EstimateBitmapBytes);
        _minimumBucketWidth = Math.Max(1, minimumBucketWidth);
        _maximumBucketWidth = Math.Max(_minimumBucketWidth, maximumBucketWidth);
        _captureSourcePixelSize = captureSourcePixelSize;
    }

    public async Task<BitmapSource> GetAsync(
        int pageIndex,
        int targetPixelWidth,
        CancellationToken cancellationToken = default)
    {
        var width = BucketWidth(targetPixelWidth);
        var key = (pageIndex, width);
        if (_cache.TryGet(key, out var cached) && cached is not null)
        {
            PerformanceTrace.Event("cache.hit", $"page={pageIndex + 1}; width={width}");
            return cached;
        }

        PerformanceTrace.Event("cache.miss", $"page={pageIndex + 1}; width={width}");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        var entry = AcquireInflight(key, pageIndex, width);

        try
        {
            var image = await entry.Task.WaitAsync(linked.Token).ConfigureAwait(false);
            _cache.Set(key, image);
            return image;
        }
        finally
        {
            ReleaseInflight(key, entry);
        }
    }

    public bool TryGetSourcePixelSize(int pageIndex, out PixelSize size)
        => _sourcePixelSizes.TryGetValue(pageIndex, out size);

    public void Prefetch(IEnumerable<int> pageIndices, int targetPixelWidth)
    {
        var pages = pageIndices
            .Distinct()
            .Where(i => i >= 0 && i < _source.Descriptor.Pages.Count)
            .ToArray();

        if (pages.Length == 0)
            return;

        CancellationTokenSource batchCts;
        CancellationTokenSource? previous;
        lock (_prefetchGate)
        {
            previous = _prefetchCts;
            batchCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _prefetchCts = batchCts;
        }

        previous?.Cancel();
        _ = PrefetchBatchAsync(pages, targetPixelWidth, batchCts);
    }

    private async Task PrefetchBatchAsync(
        IReadOnlyList<int> pageIndices,
        int targetPixelWidth,
        CancellationTokenSource batchCts)
    {
        try
        {
            var tasks = pageIndices
                .Select(index => PrefetchOneAsync(index, targetPixelWidth, batchCts.Token))
                .ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_prefetchGate)
            {
                if (ReferenceEquals(_prefetchCts, batchCts))
                    _prefetchCts = null;
            }
            batchCts.Dispose();
        }
    }

    private async Task PrefetchOneAsync(
        int pageIndex,
        int targetPixelWidth,
        CancellationToken cancellationToken)
    {
        try
        {
            await GetAsync(pageIndex, targetPixelWidth, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // Prefetch must never surface as a reader failure.
        }
    }

    private InflightLoad AcquireInflight((int Page, int Width) key, int pageIndex, int width)
    {
        while (true)
        {
            if (_inflight.TryGetValue(key, out var existing))
            {
                if (existing.TryAddWaiter())
                    return existing;

                _inflight.TryRemove(new KeyValuePair<(int Page, int Width), InflightLoad>(key, existing));
                continue;
            }

            var created = new InflightLoad(
                token => LoadAsync(pageIndex, width, token),
                _lifetime.Token);

            if (_inflight.TryAdd(key, created))
                return created;

            created.Dispose();
        }
    }

    private void ReleaseInflight((int Page, int Width) key, InflightLoad entry)
    {
        if (!entry.ReleaseWaiter())
            return;

        _inflight.TryRemove(new KeyValuePair<(int Page, int Width), InflightLoad>(key, entry));

        var task = entry.Task;
        if (!task.IsCompleted)
        {
            entry.Cancel();
            _ = DisposeInflightWhenCompleteAsync(entry, task);
        }
        else
        {
            entry.Dispose();
        }
    }

    private static async Task DisposeInflightWhenCompleteAsync(InflightLoad entry, Task<BitmapSource> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            entry.Dispose();
        }
    }

    private async Task<BitmapSource> LoadAsync(
        int pageIndex,
        int targetPixelWidth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var readStartedAt = PerformanceTrace.Start();
        var bytes = await _source.ReadPageBytesAsync(pageIndex, cancellationToken).ConfigureAwait(false);
        PerformanceTrace.Elapsed("page.read", readStartedAt, $"page={pageIndex + 1}; bytes={bytes.Length}");

        cancellationToken.ThrowIfCancellationRequested();

        var decodeStartedAt = PerformanceTrace.Start();
        var result = await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_captureSourcePixelSize)
            {
                var probeStartedAt = PerformanceTrace.Start();
                var sourceSize = _decoder.Probe(bytes);
                _sourcePixelSizes[pageIndex] = sourceSize;
                PerformanceTrace.Elapsed(
                    "page.probe",
                    probeStartedAt,
                    $"page={pageIndex + 1}; source={sourceSize.Width}x{sourceSize.Height}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var bitmapDecodeStartedAt = PerformanceTrace.Start();
            var bitmap = _decoder.Decode(bytes, targetPixelWidth);
            PerformanceTrace.Elapsed(
                "page.bitmap-decode",
                bitmapDecodeStartedAt,
                $"page={pageIndex + 1}; target={targetPixelWidth}; decoded={bitmap.PixelWidth}x{bitmap.PixelHeight}");
            return bitmap;
        }, cancellationToken).ConfigureAwait(false);
        PerformanceTrace.Elapsed("page.decode", decodeStartedAt, $"page={pageIndex + 1}; target={targetPixelWidth}");
        return result;
    }

    private static long EstimateBitmapBytes(BitmapSource image)
    {
        var bitsPerPixel = Math.Max(1, image.Format.BitsPerPixel);
        return checked((long)image.PixelWidth * image.PixelHeight * bitsPerPixel / 8);
    }

    private int BucketWidth(int width)
    {
        if (width <= 0) return 0;
        width = Math.Clamp(width, _minimumBucketWidth, _maximumBucketWidth);
        const int bucket = 64;
        return ((width + bucket - 1) / bucket) * bucket;
    }

    public void Dispose()
    {
        _lifetime.Cancel();

        lock (_prefetchGate)
        {
            _prefetchCts?.Cancel();
            _prefetchCts = null;
        }

        foreach (var entry in _inflight.Values)
            entry.Cancel();

        _lifetime.Dispose();
        _cache.Clear();
        _sourcePixelSizes.Clear();
    }

    private sealed class InflightLoad : IDisposable
    {
        private readonly object _gate = new();
        private readonly CancellationTokenSource _cts;
        private readonly Lazy<Task<BitmapSource>> _task;
        private int _waiters = 1;
        private bool _acceptingWaiters = true;
        private int _disposed;

        public InflightLoad(
            Func<CancellationToken, Task<BitmapSource>> loader,
            CancellationToken lifetimeToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
            _task = new Lazy<Task<BitmapSource>>(
                () => loader(_cts.Token),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task<BitmapSource> Task => _task.Value;

        public bool TryAddWaiter()
        {
            lock (_gate)
            {
                if (!_acceptingWaiters)
                    return false;

                _waiters++;
                return true;
            }
        }

        public bool ReleaseWaiter()
        {
            lock (_gate)
            {
                if (_waiters > 0)
                    _waiters--;

                if (_waiters != 0)
                    return false;

                _acceptingWaiters = false;
                return true;
            }
        }

        public void Cancel()
        {
            try { _cts.Cancel(); } catch (ObjectDisposedException) { }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _cts.Dispose();
        }
    }
}
