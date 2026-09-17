namespace Comet.Core.Models;

public sealed record PageDescriptor(
    int Index,
    string Name,
    string Extension,
    long UncompressedLength);

public sealed record BookDescriptor(
    string Id,
    string Path,
    string DisplayName,
    IReadOnlyList<PageDescriptor> Pages);

public sealed record Bookmark(int PageIndex, DateTimeOffset CreatedAt, string? Label = null);

public sealed record BookState(
    int LastPageIndex,
    IReadOnlyList<Bookmark> Bookmarks,
    long SourceLength,
    DateTimeOffset SourceLastWriteTimeUtc)
{
    public static BookState Empty => new(0, Array.Empty<Bookmark>(), 0, DateTimeOffset.MinValue);
}
