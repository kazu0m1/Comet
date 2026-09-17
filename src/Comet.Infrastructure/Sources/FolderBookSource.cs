using Comet.Core.Abstractions;
using Comet.Core.Models;
using Comet.Core.Services;

namespace Comet.Infrastructure.Sources;

public sealed class FolderBookSource : IBookSource
{
    private readonly IReadOnlyList<string> _files;

    private FolderBookSource(string path, IReadOnlyList<string> files)
    {
        _files = files;
        Descriptor = new BookDescriptor(
            path,
            path,
            new DirectoryInfo(path).Name,
            files.Select((f, i) => new PageDescriptor(i, Path.GetFileName(f), Path.GetExtension(f), new FileInfo(f).Length)).ToArray());
    }

    public BookDescriptor Descriptor { get; }

    public static ValueTask<FolderBookSource> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        var files = Directory.EnumerateFiles(fullPath, "*", SearchOption.TopDirectoryOnly)
            .Where(SupportedImages.IsSupported)
            .OrderBy(f => Path.GetFileName(f), NaturalStringComparer.Instance)
            .ToArray();
        return ValueTask.FromResult(new FolderBookSource(fullPath, files));
    }

    public ValueTask<byte[]> ReadPageBytesAsync(int pageIndex, CancellationToken cancellationToken = default)
    {
        if ((uint)pageIndex >= (uint)_files.Count)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        return new ValueTask<byte[]>(File.ReadAllBytesAsync(_files[pageIndex], cancellationToken));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
