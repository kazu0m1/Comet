using Comet.Core.Abstractions;
using Comet.Core.Models;
using Comet.Core.Services;
using SharpCompress.Archives;

namespace Comet.Infrastructure.Sources;

public sealed class ArchiveBookSource : IBookSource
{
    private const long MaxEntryBytes = 512L * 1024 * 1024;
    private readonly IArchive _archive;
    private readonly IReadOnlyList<IArchiveEntry> _entries;
    private readonly SemaphoreSlim _archiveGate = new(1, 1);
    private int _disposed;

    private ArchiveBookSource(string path, IArchive archive, IReadOnlyList<IArchiveEntry> entries)
    {
        _archive = archive;
        _entries = entries;
        Descriptor = new BookDescriptor(
            path,
            path,
            Path.GetFileNameWithoutExtension(path),
            entries.Select((entry, index) =>
            {
                var name = entry.Key ?? string.Empty;
                long length;
                try { length = entry.Size; }
                catch { length = 0; }
                return new PageDescriptor(index, name, Path.GetExtension(name), length);
            }).ToArray());
    }

    public BookDescriptor Descriptor { get; }

    public static ValueTask<ArchiveBookSource> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        IArchive? archive = null;
        try
        {
            archive = ArchiveFactory.OpenArchive(fullPath);
            var entries = archive.Entries
                .Where(entry =>
                {
                    if (entry.IsDirectory)
                        return false;
                    var name = entry.Key;
                    return !string.IsNullOrWhiteSpace(name) && SupportedImages.IsSupported(name);
                })
                .OrderBy(entry => entry.Key ?? string.Empty, NaturalStringComparer.Instance)
                .ToArray();

            return ValueTask.FromResult(new ArchiveBookSource(fullPath, archive, entries));
        }
        catch
        {
            archive?.Dispose();
            throw;
        }
    }

    public async ValueTask<byte[]> ReadPageBytesAsync(int pageIndex, CancellationToken cancellationToken = default)
    {
        if ((uint)pageIndex >= (uint)_entries.Count)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(ArchiveBookSource));

        await _archiveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(ArchiveBookSource));

            var entry = _entries[pageIndex];
            long length;
            try { length = entry.Size; }
            catch { length = 0; }

            if (length > MaxEntryBytes)
                throw new InvalidDataException("Image entry exceeds the 512 MiB safety limit.");

            await using var input = await entry.OpenEntryStreamAsync(cancellationToken).ConfigureAwait(false);
            using var output = length is > 0 and <= int.MaxValue
                ? new MemoryStream((int)length)
                : new MemoryStream();
            await input.CopyToAsync(output, 128 * 1024, cancellationToken).ConfigureAwait(false);

            if (output.Length > MaxEntryBytes)
                throw new InvalidDataException("Image entry exceeds the 512 MiB safety limit.");

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
        }
        finally
        {
            _archiveGate.Release();
        }
    }
}
