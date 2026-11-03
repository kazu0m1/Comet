using System.Windows;
using System.Windows.Input;
using Comet.Infrastructure.Sources;
using Comet.Core.Services;

namespace Comet.App;

public partial class MainWindow
{
    private readonly Dictionary<int, PdfReply> _pdfVisibleText = new();
    private (int Page, int Index)? _pdfSelectionAnchor;
    private (int Page, int Index)? _pdfSelectionEnd;
    private bool _pdfSelecting;

    private async Task LoadPdfTextGeometryAsync(int pageIndex, int pageCount, CancellationToken token)
    {
        if (_book is not PdfBookSource pdf) return;
        try
        {
            for (var i = 0; i < pageCount; i++)
            {
                token.ThrowIfCancellationRequested();
                var index = pageIndex + i;
                if (_pdfVisibleText.ContainsKey(index)) continue;
                var result = await pdf.GetTextAsync(index, true, token).ConfigureAwait(true);
                if (!ReferenceEquals(_book, pdf) || token.IsCancellationRequested) return;
                _pdfVisibleText[index] = result;
                // In continuous mode, multiple realized rows can be visible
                // simultaneously. Do not evict their text geometry merely
                // because a nearby row has finished loading more recently.
                var visiblePages = IsContinuousPdfView
                    ? _continuousRows.Where(row => row.Realized)
                        .SelectMany(row => row.SecondPage is int second
                            ? new[] { row.FirstPage, second }
                            : new[] { row.FirstPage })
                        .ToHashSet()
                    : new HashSet<int>();
                foreach (var stale in _pdfVisibleText.Keys.ToArray())
                {
                    if (IsContinuousPdfView)
                    {
                        if (_pdfVisibleText.Count <= 36 || visiblePages.Contains(stale)
                            || Math.Abs(stale - pageIndex) <= 8)
                            continue;
                    }
                    else if (Math.Abs(stale - pageIndex) <= 3) continue;
                    _pdfVisibleText.Remove(stale);
                }
            }
            UpdatePdfHighlightRects();
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* Unsupported/malformed text never prevents raster reading. */ }
    }

    private bool TryHitPdfCharacter(Point screen, out (int Page, int Index) hit)
    {
        hit = default;
        if (_book is not PdfBookSource pdf) return false;
        for (var offset = 0; offset < _displayedPageCount; offset++)
        {
            var index = _pageIndex + offset;
            var display = Viewport.GetPageRect(offset);
            if (!display.Contains(screen) || !_pdfVisibleText.TryGetValue(index, out var data) || data.Boxes is null)
                continue;
            var dims = pdf.Pages[index];
            if (dims.Width <= 0 || dims.Height <= 0) continue;
            var unCropped = pdf.GetCrop(index).ToOriginal(
                (screen.X - display.Left) / display.Width,
                (screen.Y - display.Top) / display.Height);
            var normalized = PdfCoordinateMapper.DisplayToPdf(
                unCropped.U, unCropped.V, pdf.GetPageRotation(index));
            var pdfX = normalized.X * dims.Width;
            var pdfY = normalized.Y * dims.Height;
            foreach (var c in data.Boxes)
            {
                if (pdfX >= c.Left - 1 && pdfX <= c.Right + 1 &&
                    pdfY >= c.Bottom - 1 && pdfY <= c.Top + 1)
                {
                    hit = (index, c.Index);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryBeginPdfSelection(Point point)
    {
        if (!IsPdf || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) return false;
        if (!TryHitPdfCharacter(point, out var hit)) return false;
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || _pdfSelectionAnchor is null)
            _pdfSelectionAnchor = hit;
        _pdfSelectionEnd = hit;
        _pdfSelecting = true;
        UpdatePdfHighlightRects();
        return true;
    }

    private bool MovePdfSelection(Point point)
    {
        if (!_pdfSelecting) return false;
        if (TryHitPdfCharacter(point, out var hit))
        {
            _pdfSelectionEnd = hit;
            UpdatePdfHighlightRects();
        }
        return true;
    }

    private bool EndPdfSelection()
    {
        if (!_pdfSelecting) return false;
        _pdfSelecting = false;
        return true;
    }

    // Escape removes only the selection. Keep text geometry cached so the next
    // drag can immediately select text without another PDFium roundtrip.
    private void ClearPdfSelectionRange()
    {
        _pdfSelecting = false;
        if (Mouse.Captured == PdfContinuousList || Mouse.Captured == Viewport
            || Mouse.Captured is System.Windows.Controls.Image)
            Mouse.Capture(null);
        _pdfSelectionAnchor = null;
        _pdfSelectionEnd = null;
        UpdatePdfHighlightRects();
    }

    private void ClearPdfTextSelection()
    {
        ClearPdfSelectionRange();
        _pdfVisibleText.Clear();
        Viewport.PdfHighlightRects = Array.Empty<Rect>();
        Viewport.Refresh();
        UpdatePdfContinuousHighlights();
    }

    private void UpdatePdfHighlightRects()
    {
        if (_book is not PdfBookSource pdf) return;
        var rects = new List<Rect>();
        for (var offset = 0; offset < _displayedPageCount; offset++)
        {
            var page = _pageIndex + offset;
            if (!_pdfVisibleText.TryGetValue(page, out var text) || text.Boxes is null) continue;
            var viewport = Viewport.GetPageRect(offset);
            if (viewport.IsEmpty) continue;
            var dims = pdf.Pages[page];
            if (dims.Width <= 0 || dims.Height <= 0) continue;
            var selections = new List<(int Begin, int End)>();
            if (_pdfSelectionAnchor is { } anchor && _pdfSelectionEnd is { } end)
            {
                var first = anchor.Page < end.Page || (anchor.Page == end.Page && anchor.Index <= end.Index) ? anchor : end;
                var last = anchor.Equals(first) ? end : anchor;
                if (first.Page <= page && page <= last.Page)
                    selections.Add((first.Page == page ? first.Index : 0, last.Page == page ? last.Index + 1 : int.MaxValue));
            }
            foreach (var match in _pdfMatches.Where(m => m.Page == page))
                selections.Add((match.Start, match.Start + match.Length));

            foreach (var selection in selections)
                foreach (var ch in text.Boxes)
                {
                    if (ch.Index < selection.Begin || ch.Index >= selection.End) continue;
                    var c1 = PdfCoordinateMapper.PdfToDisplay(ch.Left / dims.Width, ch.Bottom / dims.Height,
                        pdf.GetPageRotation(page));
                    var c2 = PdfCoordinateMapper.PdfToDisplay(ch.Right / dims.Width, ch.Top / dims.Height,
                        pdf.GetPageRotation(page));
                    var crop = pdf.GetCrop(page);
                    var d1 = crop.FromOriginal(c1.U, c1.V);
                    var d2 = crop.FromOriginal(c2.U, c2.V);
                    var left = viewport.Left + Math.Clamp(Math.Min(d1.U, d2.U), 0, 1) * viewport.Width;
                    var right = viewport.Left + Math.Clamp(Math.Max(d1.U, d2.U), 0, 1) * viewport.Width;
                    var top = viewport.Top + Math.Clamp(Math.Min(d1.V, d2.V), 0, 1) * viewport.Height;
                    var bottom = viewport.Top + Math.Clamp(Math.Max(d1.V, d2.V), 0, 1) * viewport.Height;
                    if (double.IsFinite(left) && double.IsFinite(top) && right > left && bottom > top)
                        rects.Add(new Rect(left, top, right - left, bottom - top));
                }
        }
        Viewport.PdfHighlightRects = rects;
        Viewport.Refresh();
        UpdatePdfContinuousHighlights();
    }

    private async Task CopyPdfSelectionAsync()
    {
        if (_book is not PdfBookSource pdf || _pdfSelectionAnchor is not { } a || _pdfSelectionEnd is not { } z)
            return;
        if (!pdf.CopyAllowed)
        {
            MessageBox.Show(this, "This PDF does not permit text copying.", "Comet");
            return;
        }
        var start = a.Page < z.Page || (a.Page == z.Page && a.Index <= z.Index) ? a : z;
        var end = start.Equals(a) ? z : a;
        var parts = new List<string>();
        try
        {
            for (var page = start.Page; page <= end.Page; page++)
            {
                PdfReply data;
                if (!_pdfVisibleText.TryGetValue(page, out var cached))
                    data = await pdf.GetTextAsync(page, false);
                else data = cached;
                if (!ReferenceEquals(_book, pdf)) return;
                var content = data.Text ?? "";
                var from = page == start.Page ? Math.Clamp(start.Index, 0, content.Length) : 0;
                var until = page == end.Page ? Math.Clamp(end.Index + 1, from, content.Length) : content.Length;
                parts.Add(content.Substring(from, until - from));
            }
            if (parts.Count > 0) Clipboard.SetText(string.Join(Environment.NewLine, parts));
        }
        catch (Exception ex) { MessageBox.Show(this, "Cannot copy text: " + ex.Message, "Comet"); }
    }
}
