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
    private readonly ConcurrentDictionary<(int Page, int Width), Lazy<Task<BitmapSource>>> _inflight = new();
    private readonly ConcurrentDictionary<int, PixelSize> _sourcePixelSizes = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly int _minimumBucketWidth;
    private readonly int _maximumBucketWidth;
    private readonly bool _captureSourcePixelSize;

    public PageImageCache(
        IBookSource source,
        WpfBitmapDecoder decoder,
        int capacity = 6,
        int minimumBucketWidth = 512,
        int maximumBucketWidth = 4096,
        bool captureSourcePixelSize = true)
    {
        _source = source;
        _decoder = decoder;
        _cache = new LruCache<(int Page, int Width), BitmapSource>(Math.Max(1, capacity));
        _minimumBucketWidth = Math.Max(1, minimumBucketWidth);
        _maximumBucketWidth = Math.Max(_minimumBucketWidth, maximumBucketWidth);
        _captureSourcePixelSize = captureSourcePixelSize;
    }

    public async Task<BitmapSource> GetAsync(int pageIndex, int targetPixelWidth, CancellationToken cancellationToken = default)
    {
        var width = BucketWidth(targetPixelWidth);
        var key = (pageIndex, width);
        if (_cache.TryGet(key, out var cached) && cached is not null)
            return cached;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<BitmapSource>>(
            () => LoadAsync(pageIndex, width, _lifetime.Token), LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var image = await lazy.Value.WaitAsync(linked.Token).ConfigureAwait(false);
            _cache.Set(key, image);
            return image;
        }
        finally
        {
            if (lazy.IsValueCreated && lazy.Value.IsCompleted)
                _inflight.TryRemove(key, out _);
        }
    }

    public bool TryGetSourcePixelSize(int pageIndex, out PixelSize size)
        => _sourcePixelSizes.TryGetValue(pageIndex, out size);

    public void Prefetch(IEnumerable<int> pageIndices, int targetPixelWidth)
    {
        foreach (var index in pageIndices.Distinct().Where(i => i >= 0 && i < _source.Descriptor.Pages.Count))
            _ = PrefetchOneAsync(index, targetPixelWidth);
    }

    private async Task PrefetchOneAsync(int pageIndex, int targetPixelWidth)
    {
        try { await GetAsync(pageIndex, targetPixelWidth, _lifetime.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch { /* Prefetch must never surface as a reader failure. */ }
    }

    private async Task<BitmapSource> LoadAsync(int pageIndex, int targetPixelWidth, CancellationToken cancellationToken)
    {
        var bytes = await _source.ReadPageBytesAsync(pageIndex, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            if (_captureSourcePixelSize)
                _sourcePixelSizes[pageIndex] = _decoder.Probe(bytes);
            return _decoder.Decode(bytes, targetPixelWidth);
        }, cancellationToken).ConfigureAwait(false);
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
        _lifetime.Dispose();
        _cache.Clear();
        _sourcePixelSizes.Clear();
    }
}
