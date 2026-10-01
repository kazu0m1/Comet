using System.IO.Compression;
using System.Text;
using Comet.Core.Abstractions;
using Comet.Core.Models;
using Comet.Core.Services;

namespace Comet.Infrastructure.Sources;

public sealed class ZipBookSource : IBookSource
{
    private const long MaxEntryBytes = 512L * 1024 * 1024;
    private const long MaxNestedArchiveBytes = 8L * 1024 * 1024 * 1024;
    private const long MaxTotalNestedArchiveBytes = 32L * 1024 * 1024 * 1024;
    private const int MaxNestedDepth = 16;

    private readonly IReadOnlyList<ArchiveHandle> _archives;
    private readonly IReadOnlyList<PageEntry> _entries;
    private readonly string? _tempDirectory;
    private readonly SemaphoreSlim _archiveGate = new(1, 1);
    private int _disposed;

    private ZipBookSource(string path, BuildState state)
    {
        _archives = state.Archives.ToArray();
        _entries = state.Pages.ToArray();
        _tempDirectory = state.TempDirectory;
        state.TransferOwnership();

        Descriptor = new BookDescriptor(
            path,
            path,
            Path.GetFileNameWithoutExtension(path),
            _entries.Select((entry, index) =>
                new PageDescriptor(index, entry.DisplayName, Path.GetExtension(entry.DisplayName), entry.Length))
                .ToArray());
    }

    public BookDescriptor Descriptor { get; }

    public static async ValueTask<ZipBookSource> OpenAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        var state = new BuildState();

        try
        {
            var root = OpenArchiveHandle(fullPath);
            state.Archives.Add(root);
            await CollectPagesAsync(root.Archive, prefix: string.Empty, depth: 0, state, cancellationToken)
                .ConfigureAwait(false);
            return new ZipBookSource(fullPath, state);
        }
        catch
        {
            state.Dispose();
            throw;
        }
    }

    private static async Task CollectPagesAsync(
        ZipArchive archive,
        string prefix,
        int depth,
        BuildState state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .OrderBy(entry => entry.FullName, NaturalStringComparer.Instance)
            .ToArray();

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalizedName = NormalizeEntryName(entry.FullName);

            if (SupportedImages.IsSupported(normalizedName))
            {
                state.Pages.Add(new PageEntry(entry, prefix + normalizedName, entry.Length));
                continue;
            }

            if (!SupportedArchives.UsesSystemZip(normalizedName))
                continue;

            if (depth >= MaxNestedDepth)
                throw new InvalidDataException($"Nested ZIP depth exceeds the supported limit of {MaxNestedDepth}.");

            var nestedPath = await ExtractNestedArchiveAsync(entry, state, cancellationToken)
                .ConfigureAwait(false);
            var nested = OpenArchiveHandle(nestedPath);
            state.Archives.Add(nested);

            await CollectPagesAsync(
                    nested.Archive,
                    prefix + normalizedName + "/",
                    depth + 1,
                    state,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<string> ExtractNestedArchiveAsync(
        ZipArchiveEntry entry,
        BuildState state,
        CancellationToken cancellationToken)
    {
        if (entry.Length > MaxNestedArchiveBytes)
            throw new InvalidDataException("Nested ZIP entry exceeds the 8 GiB safety limit.");

        if (entry.Length > MaxTotalNestedArchiveBytes - state.TotalNestedArchiveBytes)
            throw new InvalidDataException("Nested ZIP entries exceed the 32 GiB temporary-storage safety limit.");

        var tempDirectory = state.EnsureTempDirectory();
        var extension = Path.GetExtension(entry.Name);
        var tempPath = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}{extension}");

        await using var input = entry.Open();
        await using var output = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var buffer = new byte[128 * 1024];
        long copied = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;

            copied += read;
            if (copied > MaxNestedArchiveBytes)
                throw new InvalidDataException("Nested ZIP entry exceeds the 8 GiB safety limit.");
            if (copied > MaxTotalNestedArchiveBytes - state.TotalNestedArchiveBytes)
                throw new InvalidDataException("Nested ZIP entries exceed the 32 GiB temporary-storage safety limit.");

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        state.TotalNestedArchiveBytes += copied;
        return tempPath;
    }

    private static ArchiveHandle OpenArchiveHandle(string path)
    {
        var handle = OpenArchiveHandle(path, entryNameEncoding: null);
        if (!handle.Archive.Entries.Any(entry => entry.FullName.Contains('\uFFFD')))
            return handle;

        handle.Dispose();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return OpenArchiveHandle(path, Encoding.GetEncoding(932));
    }

    private static ArchiveHandle OpenArchiveHandle(string path, Encoding? entryNameEncoding)
    {
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.RandomAccess);

        try
        {
            var archive = new ZipArchive(
                stream,
                ZipArchiveMode.Read,
                leaveOpen: true,
                entryNameEncoding: entryNameEncoding);
            return new ArchiveHandle(stream, archive);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public async ValueTask<byte[]> ReadPageBytesAsync(
        int pageIndex,
        CancellationToken cancellationToken = default)
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

            var page = _entries[pageIndex];
            if (page.Length > MaxEntryBytes)
                throw new InvalidDataException("Image entry exceeds the 512 MiB safety limit.");

            await using var input = page.Entry.Open();
            using var output = page.Length is > 0 and <= int.MaxValue
                ? new MemoryStream((int)page.Length)
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
            for (var i = _archives.Count - 1; i >= 0; i--)
            {
                try { _archives[i].Dispose(); }
                catch { }
            }
        }
        finally
        {
            _archiveGate.Release();
            TryDeleteTempDirectory(_tempDirectory);
        }
    }

    private static string NormalizeEntryName(string name)
        => name.Replace('\\', '/');

    private static void TryDeleteTempDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        try { Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed record PageEntry(ZipArchiveEntry Entry, string DisplayName, long Length);

    private sealed class ArchiveHandle(FileStream stream, ZipArchive archive) : IDisposable
    {
        public FileStream Stream { get; } = stream;
        public ZipArchive Archive { get; } = archive;

        public void Dispose()
        {
            Archive.Dispose();
            Stream.Dispose();
        }
    }

    private sealed class BuildState : IDisposable
    {
        private bool _ownershipTransferred;

        public List<ArchiveHandle> Archives { get; } = [];
        public List<PageEntry> Pages { get; } = [];
        public string? TempDirectory { get; private set; }
        public long TotalNestedArchiveBytes { get; set; }

        public string EnsureTempDirectory()
        {
            if (TempDirectory is not null)
                return TempDirectory;

            var root = Path.Combine(Path.GetTempPath(), "Comet", "nested-zips");
            Directory.CreateDirectory(root);
            TempDirectory = Path.Combine(root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(TempDirectory);
            return TempDirectory;
        }

        public void TransferOwnership() => _ownershipTransferred = true;

        public void Dispose()
        {
            if (_ownershipTransferred)
                return;

            for (var i = Archives.Count - 1; i >= 0; i--)
            {
                try { Archives[i].Dispose(); }
                catch { }
            }
            TryDeleteTempDirectory(TempDirectory);
        }
    }
}
