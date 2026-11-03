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
    DateTimeOffset SourceLastWriteTimeUtc,
    PdfViewState? Pdf = null)
{
    public static BookState Empty => new(0, Array.Empty<Bookmark>(), 0, DateTimeOffset.MinValue);
}

// Optional per-PDF preferences are stored in the existing book-state file.
// Old book-state JSON remains readable because Pdf defaults to null.
public sealed record PdfViewState(
    ReadingDirection ReadingDirection = ReadingDirection.LeftToRight,
    PageLayoutMode PageLayoutMode = PageLayoutMode.DoublePage,
    FitMode FitMode = FitMode.BestFit,
    bool CoverAlone = true,
    bool ContinuousScroll = false,
    bool AutoCrop = false,
    int RotationAll = 0,
    Dictionary<int,int>? PageRotations = null);
