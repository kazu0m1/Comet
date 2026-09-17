using Comet.Core.Models;

namespace Comet.Core.Services;

public static class SpreadPlanner
{
    public static int NormalizeStartIndex(int requestedIndex, int pageCount, PageLayoutMode layout)
    {
        if (pageCount <= 0) return 0;
        var index = Math.Clamp(requestedIndex, 0, pageCount - 1);
        if (layout == PageLayoutMode.SinglePage || index == 0) return index;

        // Cover is page 0. Standard double-page spreads start at 1,3,5,...
        return index % 2 == 0 ? index - 1 : index;
    }

    public static int NextStartIndex(int currentStart, int displayedPageCount, int totalPages)
    {
        if (totalPages <= 0) return 0;
        return Math.Min(totalPages - 1, currentStart + Math.Max(1, displayedPageCount));
    }

    public static int PreviousStartIndex(int currentStart, PageLayoutMode layout)
    {
        if (currentStart <= 0) return 0;
        if (layout == PageLayoutMode.SinglePage) return currentStart - 1;
        if (currentStart <= 1) return 0;
        return Math.Max(1, currentStart - 2);
    }
}
