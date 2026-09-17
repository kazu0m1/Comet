using Comet.Core.Models;

namespace Comet.Core.Abstractions;

public interface IBookSource : IAsyncDisposable
{
    BookDescriptor Descriptor { get; }
    ValueTask<byte[]> ReadPageBytesAsync(int pageIndex, CancellationToken cancellationToken = default);
}

public interface IBookSourceFactory
{
    ValueTask<IBookSource> OpenAsync(string path, CancellationToken cancellationToken = default);
}

public interface ISettingsStore
{
    ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    ValueTask SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public interface IBookStateStore
{
    ValueTask<BookState?> LoadAsync(string sourcePath, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(string sourcePath, BookState state, CancellationToken cancellationToken = default);
}
