using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Comet.App.Services;
using Comet.Core.Models;
using Comet.Core.Services;
using Comet.Infrastructure.Sources;

namespace Comet.App;

public partial class MainWindow
{
    private readonly ObservableCollection<PdfContinuousRow> _continuousRows = new();
    private CancellationTokenSource? _continuousCts;
    private bool _changingContinuousSelection;

    private bool IsContinuousPdfView => IsPdf && _pdfView?.ContinuousScroll == true;

    private async void PdfContinuous_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfView is null || _book is not PdfBookSource) return;
        _pdfView = _pdfView with { ContinuousScroll = !_pdfView.ContinuousScroll };
        RebuildContinuousPdfView();
        UpdateMenuChecks();
        ScheduleStateSave();
        await RenderCurrentAsync();
    }

    private void ResetContinuousPdfView()
    {
        _continuousCts?.Cancel();
        _continuousCts?.Dispose();
        _continuousCts = null;
        _continuousRows.Clear();
        PdfContinuousList.Visibility = Visibility.Collapsed;
        Viewport.Visibility = Visibility.Visible;
    }

    private void RebuildContinuousPdfView()
    {
        _continuousCts?.Cancel();
        _continuousCts?.Dispose();
        _continuousCts = null;
        _continuousRows.Clear();
        var pdf = _book as PdfBookSource;
        if (pdf is null || !IsContinuousPdfView)
        {
            PdfContinuousList.Visibility = Visibility.Collapsed;
            Viewport.Visibility = Visibility.Visible;
            return;
        }
        _continuousCts = new CancellationTokenSource();
        var count = pdf.Descriptor.Pages.Count;
        var index = 0;
        while (index < count)
        {
            int? second = null;
            var p = pdf.Pages[index];
            var landscape = (pdf.GetPageRotation(index) % 2 == 0)
                ? p.Width > p.Height : p.Height > p.Width;
            if (CurrentPageLayoutMode == PageLayoutMode.DoublePage &&
                (index > 0 || !CoverAlone) &&
                index + 1 < count && !landscape)
            {
                var q = pdf.Pages[index + 1];
                var otherLandscape = (pdf.GetPageRotation(index + 1) % 2 == 0)
                    ? q.Width > q.Height : q.Height > q.Width;
                if (!otherLandscape) second = index + 1;
            }
            var row = new PdfContinuousRow(index, second);
            UpdateContinuousRowSize(pdf, row);
            _continuousRows.Add(row);
            index += second.HasValue ? 2 : 1;
        }
        Viewport.Visibility = Visibility.Collapsed;
        PdfContinuousList.Visibility = Visibility.Visible;
        JumpContinuousToPage(_pageIndex);
    }

    private void UpdateContinuousRowSize(PdfBookSource pdf, PdfContinuousRow row)
    {
        var a = pdf.Pages[row.FirstPage];
        var aW = pdf.GetPageRotation(row.FirstPage) % 2 == 0 ? a.Width : a.Height;
        var aH = pdf.GetPageRotation(row.FirstPage) % 2 == 0 ? a.Height : a.Width;
        aW = Math.Max(aW, 1); aH = Math.Max(aH, 1);
        var bW = 0.0; var bH = 0.0;
        if (row.SecondPage is int second)
        {
            var b = pdf.Pages[second];
            bW = Math.Max(pdf.GetPageRotation(second) % 2 == 0 ? b.Width : b.Height, 1);
            bH = Math.Max(pdf.GetPageRotation(second) % 2 == 0 ? b.Height : b.Width, 1);
        }
        var maxWidth = Math.Max(320, PdfContinuousList.ActualWidth - 32);
        var maxHeight = Math.Max(300, PdfContinuousList.ActualHeight - 24);
        var combinedWidth = aW + bW + (bW > 0 ? 10 : 0);
        var combinedHeight = Math.Max(aH, bH);
        var sx = maxWidth / combinedWidth;
        var sy = maxHeight / combinedHeight;
        var scale = CurrentFitMode switch
        {
            FitMode.FitWidth => sx,
            FitMode.FitHeight => sy,
            FitMode.Manual => 96.0 / 72,
            FitMode.FixedSize => (_settings.FixedSizeAxis == FixedSizeAxis.Width
                ? _settings.FixedSizeDip / aW : _settings.FixedSizeDip / aH),
            _ => Math.Min(sx, sy)
        };
        if (!_settings.StretchSmallImages && CurrentFitMode != FitMode.FixedSize)
            scale = Math.Min(scale, 96.0 / 72);
        scale = Math.Clamp(scale * _zoom.TemporaryZoomFactor, 0.05, 24.0);
        row.FirstWidth = aW * scale;
        row.FirstHeight = aH * scale;
        if (bW > 0 && bH > 0)
        {
            var secondScale = CurrentFitMode == FitMode.FixedSize
                ? Math.Clamp((_settings.FixedSizeAxis == FixedSizeAxis.Width
                    ? _settings.FixedSizeDip / bW : _settings.FixedSizeDip / bH) *
                    _zoom.TemporaryZoomFactor, 0.05, 24.0) : scale;
            row.SecondWidth = bW * secondScale;
            row.SecondHeight = bH * secondScale;
        }
        else { row.SecondWidth = 0; row.SecondHeight = 0; }
    }

    private void JumpContinuousToPage(int page)
    {
        if (!IsContinuousPdfView || _continuousRows.Count == 0) return;
        var row = _continuousRows.FirstOrDefault(x => x.FirstPage == page || x.SecondPage == page)
            ?? _continuousRows.LastOrDefault(x => x.FirstPage <= page);
        if (row is null) return;
        _changingContinuousSelection = true;
        try { PdfContinuousList.SelectedItem = row; PdfContinuousList.ScrollIntoView(row); }
        finally { _changingContinuousSelection = false; }
    }

    private void PdfContinuousList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingContinuousSelection || !IsContinuousPdfView) return;
        if (PdfContinuousList.SelectedItem is PdfContinuousRow row && _book is not null)
        {
            _pageIndex = row.FirstPage;
            _displayedPageCount = row.SecondPage.HasValue ? 2 : 1;
            UpdateStatus();
            ScheduleStateSave();
        }
    }

    private void PdfContinuousRow_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PdfContinuousRow row })
        {
            row.Realized = true;
            _ = LoadContinuousRowAsync(row);
        }
    }

    private void PdfContinuous_Cleanup(object sender, CleanUpVirtualizedItemEventArgs e)
    {
        if (e.Value is PdfContinuousRow row)
        {
            row.Realized = false;
            row.FirstImage = null;
            row.SecondImage = null;
        }
    }

    private async Task LoadContinuousRowAsync(PdfContinuousRow row)
    {
        if (row.IsLoading || !row.Realized || _book is not PdfBookSource pdf ||
            _imageCache is null || _continuousCts is null) return;
        var cache = _imageCache;
        var token = _continuousCts.Token;
        row.IsLoading = true;
        try
        {
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(PdfContinuousList).DpiScaleX;
            var width = (int)Math.Clamp(Math.Ceiling(row.FirstWidth * dpi * 1.35), 512, 4096);
            var first = await cache.GetAsync(row.FirstPage, width, token);
            if (!token.IsCancellationRequested && row.Realized && ReferenceEquals(_book, pdf))
                row.FirstImage = first;
            if (row.SecondPage is int second)
            {
                var w = (int)Math.Clamp(Math.Ceiling(row.SecondWidth * dpi * 1.35), 512, 4096);
                var image = await cache.GetAsync(second, w, token);
                if (!token.IsCancellationRequested && row.Realized && ReferenceEquals(_book, pdf))
                    row.SecondImage = image;
            }
            await LoadPdfTextGeometryAsync(row.FirstPage,
                row.SecondPage.HasValue ? 2 : 1, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* Broken page leaves the row blank, without aborting navigation. */ }
        finally { row.IsLoading = false; }
    }

    private bool TryHitContinuousCharacter(Image image, Point point, out (int Page, int Index) hit)
    {
        hit = default;
        if (_book is not PdfBookSource pdf || image.Tag is not int page ||
            page < 0 || page >= pdf.Pages.Count ||
            image.ActualWidth <= 0 || image.ActualHeight <= 0 ||
            !_pdfVisibleText.TryGetValue(page, out var text) || text.Boxes is null)
            return false;
        var dim = pdf.Pages[page];
        if (dim.Width <= 0 || dim.Height <= 0) return false;
        var original = pdf.GetCrop(page).ToOriginal(point.X / image.ActualWidth,
            point.Y / image.ActualHeight);
        var normal = PdfCoordinateMapper.DisplayToPdf(original.U, original.V,
            pdf.GetPageRotation(page));
        var x = normal.X * dim.Width;
        var y = normal.Y * dim.Height;
        foreach (var box in text.Boxes)
        {
            if (x >= box.Left - 1 && x <= box.Right + 1 &&
                y >= box.Bottom - 1 && y <= box.Top + 1)
            {
                hit = (page, box.Index);
                return true;
            }
        }
        return false;
    }

    private void PdfContinuousImage_Down(object sender, MouseButtonEventArgs e)
    {
        if (!IsContinuousPdfView || e.ChangedButton != MouseButton.Left ||
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || sender is not Image image)
            return;
        if (!TryHitContinuousCharacter(image, e.GetPosition(image), out var hit)) return;
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || _pdfSelectionAnchor is null)
            _pdfSelectionAnchor = hit;
        _pdfSelectionEnd = hit;
        _pdfSelecting = true;
        image.CaptureMouse();
        UpdatePdfHighlightRects();
        e.Handled = true;
    }

    private void PdfContinuousImage_Move(object sender, MouseEventArgs e)
    {
        if (!_pdfSelecting || !IsContinuousPdfView || sender is not Image) return;
        var p = e.GetPosition(PdfContinuousList);
        var picked = VisualTreeHelper.HitTest(PdfContinuousList, p)?.VisualHit;
        while (picked is not null && picked is not Image)
            picked = VisualTreeHelper.GetParent(picked);
        if (picked is Image target &&
            TryHitContinuousCharacter(target, e.GetPosition(target), out var hit))
        {
            _pdfSelectionEnd = hit;
            UpdatePdfHighlightRects();
        }
        e.Handled = true;
    }

    private void PdfContinuousImage_Up(object sender, MouseButtonEventArgs e)
    {
        if (!_pdfSelecting || e.ChangedButton != MouseButton.Left) return;
        _pdfSelecting = false;
        if (Mouse.Captured is not null) Mouse.Capture(null);
        UpdatePdfHighlightRects();
        e.Handled = true;
    }

    // Each item is virtualized. Only visible rows own WPF glyph highlight elements.
    private void UpdatePdfContinuousHighlights()
    {
        if (!IsContinuousPdfView || _book is not PdfBookSource pdf) return;
        foreach (var row in _continuousRows)
        {
            if (!row.Realized) continue;
            FillContinuousHighlights(row.FirstHighlights, row.FirstPage,
                row.FirstWidth, row.FirstHeight, pdf);
            if (row.SecondPage is int other)
                FillContinuousHighlights(row.SecondHighlights, other,
                    row.SecondWidth, row.SecondHeight, pdf);
        }
    }

    private void FillContinuousHighlights(System.Collections.ObjectModel.ObservableCollection<Rect> output,
        int page, double width, double height, PdfBookSource pdf)
    {
        output.Clear();
        if (width <= 0 || height <= 0 ||
            !_pdfVisibleText.TryGetValue(page, out var data) || data.Boxes is null)
            return;
        var selections = new List<(int Start, int End)>();
        if (_pdfSelectionAnchor is { } start && _pdfSelectionEnd is { } end)
        {
            var a = start.Page < end.Page || (start.Page == end.Page && start.Index <= end.Index) ? start : end;
            var z = a.Equals(start) ? end : start;
            if (a.Page <= page && page <= z.Page)
                selections.Add((a.Page == page ? a.Index : 0, z.Page == page ? z.Index + 1 : int.MaxValue));
        }
        foreach (var match in _pdfMatches.Where(x => x.Page == page))
            selections.Add((match.Start, match.Start + match.Length));
        if (selections.Count == 0) return;
        var dim = pdf.Pages[page];
        if (dim.Width <= 0 || dim.Height <= 0) return;
        var crop = pdf.GetCrop(page);
        var turns = pdf.GetPageRotation(page);
        foreach (var c in data.Boxes)
        {
            if (!selections.Any(x => x.Start <= c.Index && c.Index < x.End)) continue;
            var a = PdfCoordinateMapper.PdfToDisplay(c.Left / dim.Width, c.Bottom / dim.Height, turns);
            var z = PdfCoordinateMapper.PdfToDisplay(c.Right / dim.Width, c.Top / dim.Height, turns);
            var aa = crop.FromOriginal(a.U, a.V);
            var zz = crop.FromOriginal(z.U, z.V);
            var l = Math.Clamp(Math.Min(aa.U, zz.U), 0, 1) * width;
            var r = Math.Clamp(Math.Max(aa.U, zz.U), 0, 1) * width;
            var t = Math.Clamp(Math.Min(aa.V, zz.V), 0, 1) * height;
            var b = Math.Clamp(Math.Max(aa.V, zz.V), 0, 1) * height;
            if (r > l && b > t && double.IsFinite(l) && double.IsFinite(t))
                output.Add(new Rect(l, t, r - l, b - t));
        }
    }

    private void PdfContinuousList_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsContinuousPdfView || _book is not PdfBookSource pdf) return;
        foreach (var row in _continuousRows)
        {
            UpdateContinuousRowSize(pdf, row);
            row.FirstImage = null;
            row.SecondImage = null;
            if (row.Realized) _ = LoadContinuousRowAsync(row);
        }
    }
}
