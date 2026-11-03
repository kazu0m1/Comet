using Comet.Core.Models;

namespace Comet.Core.Services;

public static class SpreadPlanner
{
    // The document's logical page sequence never changes. Only the physical
    // left/right columns of each visible double-page spread are reversed.
    public static (int FirstColumn, int SecondColumn) DisplayColumns(
        ReadingDirection direction, bool hasSecondPage)
        => direction == ReadingDirection.RightToLeft && hasSecondPage
            ? (1, 0) : (0, 1);

    public static int NormalizeStartIndex(int requestedIndex, int pageCount, PageLayoutMode layout, bool coverAlone = true)
    {
        if (pageCount <= 0) return 0;
        var index = Math.Clamp(requestedIndex, 0, pageCount - 1);
        if (layout == PageLayoutMode.SinglePage || (index == 0 && coverAlone)) return index;
        // When the cover stands alone, pairs start at 1,3,...; otherwise at 0,2,...
        return coverAlone ? (index % 2 == 0 ? index - 1 : index) : (index & ~1);
    }

    public static int NextStartIndex(int currentStart, int displayedPageCount, int totalPages)
    {
        if (totalPages <= 0) return 0;
        return Math.Min(totalPages - 1, currentStart + Math.Max(1, displayedPageCount));
    }

    public static int PreviousStartIndex(int currentStart, PageLayoutMode layout, bool coverAlone = true)
    {
        if (currentStart <= 0) return 0;
        if (layout == PageLayoutMode.SinglePage) return currentStart - 1;
        if (!coverAlone) return Math.Max(0, currentStart - 2);
        if (currentStart <= 1) return 0;
        return Math.Max(1, currentStart - 2);
    }
}
