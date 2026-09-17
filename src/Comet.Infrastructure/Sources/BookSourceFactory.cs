using Comet.Core.Abstractions;

namespace Comet.Infrastructure.Sources;

public sealed class BookSourceFactory : IBookSourceFactory
{
    public async ValueTask<IBookSource> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
            return await FolderBookSource.OpenAsync(fullPath, cancellationToken);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Book source not found.", fullPath);

        if (SupportedImages.IsSupported(fullPath))
            return await FolderBookSource.OpenAsync(Path.GetDirectoryName(fullPath)!, cancellationToken);

        return Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".zip" or ".cbz" => await ZipBookSource.OpenAsync(fullPath, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported source: {Path.GetExtension(fullPath)}")
        };
    }
}
