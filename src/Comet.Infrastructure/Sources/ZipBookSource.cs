using System.IO.Compression;
using System.Text;
using Comet.Core.Abstractions;
using Comet.Core.Models;
using Comet.Core.Services;

namespace Comet.Infrastructure.Sources;

public sealed class ZipBookSource : IBookSource
{
    private const long MaxEntryBytes = 512L * 1024 * 1024;
    private readonly FileStream _fileStream;
    private readonly ZipArchive _archive;
    private readonly IReadOnlyList<ZipArchiveEntry> _entries;
    private readonly SemaphoreSlim _archiveGate = new(1, 1);
    private int _disposed;

    private ZipBookSource(string path, FileStream fileStream, ZipArchive archive, IReadOnlyList<ZipArchiveEntry> entries)
    {
        _fileStream = fileStream;
        _archive = archive;
        _entries = entries;
        Descriptor = new BookDescriptor(
            path,
            path,
            Path.GetFileNameWithoutExtension(path),
            entries.Select((e, i) => new PageDescriptor(i, e.FullName, Path.GetExtension(e.FullName), e.Length)).ToArray());
    }

    public BookDescriptor Descriptor { get; }

    public static ValueTask<ZipBookSource> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        var source = OpenInternal(fullPath, null);

        if (source._entries.Any(e => e.FullName.Contains('\uFFFD')))
        {
            source.DisposeAsync().AsTask().GetAwaiter().GetResult();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            source = OpenInternal(fullPath, Encoding.GetEncoding(932));
        }

        return ValueTask.FromResult(source);
    }

    private static ZipBookSource OpenInternal(string path, Encoding? entryNameEncoding)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024,
            FileOptions.Asynchronous | FileOptions.RandomAccess);
        try
        {
            var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: entryNameEncoding);
            var entries = archive.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name) && SupportedImages.IsSupported(e.FullName))
                .OrderBy(e => e.FullName, NaturalStringComparer.Instance)
                .ToArray();
            return new ZipBookSource(path, stream, archive, entries);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public async ValueTask<byte[]> ReadPageBytesAsync(int pageIndex, CancellationToken cancellationToken = default)
    {
        if ((uint)pageIndex >= (uint)_entries.Count)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(ZipBookSource));

        await _archiveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(ZipBookSource));
            var entry = _entries[pageIndex];
            if (entry.Length > MaxEntryBytes)
                throw new InvalidDataException("Image entry exceeds the 512 MiB safety limit.");

            await using var input = entry.Open();
            using var output = entry.Length is > 0 and <= int.MaxValue
                ? new MemoryStream((int)entry.Length)
                : new MemoryStream();
            await input.CopyToAsync(output, 128 * 1024, cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }
        finally
        {
            _archiveGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await _archiveGate.WaitAsync().ConfigureAwait(false);
        try
        {
            _archive.Dispose();
            _fileStream.Dispose();
        }
        finally
        {
            _archiveGate.Release();
        }
    }
}
