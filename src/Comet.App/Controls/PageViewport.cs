using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Comet.Core.Models;
using Comet.Core.Services;

namespace Comet.App.Controls;

public sealed class PageViewport : FrameworkElement
{
    private double _offsetX;
    private double _offsetY;

    public BitmapSource? FirstPage { get; set; }
    public BitmapSource? SecondPage { get; set; }
    public string? ErrorMessage { get; set; }
    public FitMode FitMode { get; set; } = FitMode.BestFit;
    public ReadingDirection ReadingDirection { get; set; } = ReadingDirection.RightToLeft;
    public bool StretchSmallImages { get; set; } = true;
    public double TemporaryZoomFactor { get; set; } = 1.0;
    public double Gap { get; set; } = 0;

    public double CurrentFirstPageDisplayWidthDip
        => FirstPage is null ? 0 : GetLayout().FirstWidth;

    public PageViewport()
    {
        Focusable = true;
        SnapsToDevicePixels = true;
        ClipToBounds = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brushes.Black, null, new Rect(RenderSize));

        if (FirstPage is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                DrawError(dc, ErrorMessage);
            return;
        }

        var layout = GetLayout();
        ClampOffsets(layout.ContentWidth, layout.ContentHeight);

        var originX = layout.ContentWidth <= ActualWidth ? (ActualWidth - layout.ContentWidth) / 2 : -_offsetX;
        var originY = layout.ContentHeight <= ActualHeight ? (ActualHeight - layout.ContentHeight) / 2 : -_offsetY;

        var firstRect = new Rect(0, 0, layout.FirstWidth, layout.FirstHeight);
        if (SecondPage is null)
        {
            firstRect.Location = new Point(originX, originY + (layout.ContentHeight - firstRect.Height) / 2);
            dc.DrawImage(FirstPage, firstRect);
            return;
        }

        var secondRect = new Rect(0, 0, layout.SecondWidth, layout.SecondHeight);
        var firstOnRight = ReadingDirection == ReadingDirection.RightToLeft;
        var firstX = firstOnRight ? originX + secondRect.Width + layout.GapScaled : originX;
        var secondX = firstOnRight ? originX : originX + firstRect.Width + layout.GapScaled;
        firstRect.Location = new Point(firstX, originY + (layout.ContentHeight - firstRect.Height) / 2);
        secondRect.Location = new Point(secondX, originY + (layout.ContentHeight - secondRect.Height) / 2);
        dc.DrawImage(FirstPage, firstRect);
        dc.DrawImage(SecondPage, secondRect);
    }

    public void ResetScroll()
    {
        if (FirstPage is null)
        {
            _offsetX = 0;
            _offsetY = 0;
            Refresh();
            return;
        }

        var layout = GetLayout();
        var maxX = Math.Max(0, layout.ContentWidth - ActualWidth);
        _offsetX = ReadingDirection == ReadingDirection.RightToLeft ? maxX : 0;
        _offsetY = 0;
        Refresh();
    }

    public bool ScrollBy(double dx, double dy)
    {
        if (FirstPage is null) return false;
        var layout = GetLayout();
        var maxX = Math.Max(0, layout.ContentWidth - ActualWidth);
        var maxY = Math.Max(0, layout.ContentHeight - ActualHeight);
        var oldX = _offsetX;
        var oldY = _offsetY;
        _offsetX = Math.Clamp(_offsetX + dx, 0, maxX);
        _offsetY = Math.Clamp(_offsetY + dy, 0, maxY);
        var moved = Math.Abs(oldX - _offsetX) > 0.1 || Math.Abs(oldY - _offsetY) > 0.1;
        if (moved) Refresh();
        return moved;
    }

    public bool SmartScroll(int direction, double fraction)
    {
        if (FirstPage is null || direction == 0) return false;
        var layout = GetLayout();
        var maxX = Math.Max(0, layout.ContentWidth - ActualWidth);
        var maxY = Math.Max(0, layout.ContentHeight - ActualHeight);
        const double eps = 0.5;
        var stepX = Math.Max(50, ActualWidth * Math.Clamp(fraction, 0.1, 1.0));
        var stepY = Math.Max(50, ActualHeight * Math.Clamp(fraction, 0.1, 1.0));

        if (direction > 0)
        {
            if (maxX > eps)
            {
                if (ReadingDirection == ReadingDirection.RightToLeft)
                {
                    if (_offsetX > eps) return ScrollBy(-stepX, 0);
                    if (_offsetY < maxY - eps)
                    {
                        _offsetY = Math.Min(maxY, _offsetY + stepY);
                        _offsetX = maxX;
                        Refresh();
                        return true;
                    }
                }
                else
                {
                    if (_offsetX < maxX - eps) return ScrollBy(stepX, 0);
                    if (_offsetY < maxY - eps)
                    {
                        _offsetY = Math.Min(maxY, _offsetY + stepY);
                        _offsetX = 0;
                        Refresh();
                        return true;
                    }
                }
            }
            else if (_offsetY < maxY - eps)
            {
                return ScrollBy(0, stepY);
            }
        }
        else
        {
            if (maxX > eps)
            {
                if (ReadingDirection == ReadingDirection.RightToLeft)
                {
                    if (_offsetX < maxX - eps) return ScrollBy(stepX, 0);
                    if (_offsetY > eps)
                    {
                        _offsetY = Math.Max(0, _offsetY - stepY);
                        _offsetX = 0;
                        Refresh();
                        return true;
                    }
                }
                else
                {
                    if (_offsetX > eps) return ScrollBy(-stepX, 0);
                    if (_offsetY > eps)
                    {
                        _offsetY = Math.Max(0, _offsetY - stepY);
                        _offsetX = maxX;
                        Refresh();
                        return true;
                    }
                }
            }
            else if (_offsetY > eps)
            {
                return ScrollBy(0, -stepY);
            }
        }

        return false;
    }

    public void Refresh() => InvalidateVisual();

    private LayoutMetrics GetLayout()
    {
        var first = new PixelSize(FirstPage!.PixelWidth, FirstPage.PixelHeight);
        PixelSize? second = SecondPage is null ? null : new PixelSize(SecondPage.PixelWidth, SecondPage.PixelHeight);
        var combined = FitCalculator.CombineSpread(first, second, (int)Gap);
        var dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var manualScale = FitMode == FitMode.Manual
            ? DpiScaleCalculator.ManualDipScale(dpiScale)
            : 1.0;
        var baseScale = FitCalculator.CalculateScale(
            combined,
            new ViewportSize(Math.Max(1, ActualWidth), Math.Max(1, ActualHeight)),
            FitMode,
            StretchSmallImages,
            manualScale);
        var scale = baseScale * TemporaryZoomFactor;
        var firstWidth = first.Width * scale;
        var firstHeight = first.Height * scale;
        var secondWidth = second?.Width * scale ?? 0;
        var secondHeight = second?.Height * scale ?? 0;
        var gapScaled = SecondPage is null ? 0 : Gap * scale;
        return new LayoutMetrics(
            firstWidth + secondWidth + gapScaled,
            Math.Max(firstHeight, secondHeight),
            firstWidth,
            firstHeight,
            secondWidth,
            secondHeight,
            gapScaled);
    }

    private void ClampOffsets(double contentWidth, double contentHeight)
    {
        _offsetX = Math.Clamp(_offsetX, 0, Math.Max(0, contentWidth - ActualWidth));
        _offsetY = Math.Clamp(_offsetY, 0, Math.Max(0, contentHeight - ActualHeight));
    }

    private void DrawError(DrawingContext dc, string message)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var text = new FormattedText(
            message,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            16,
            Brushes.White,
            dpi)
        {
            MaxTextWidth = Math.Max(1, ActualWidth - 80),
            TextAlignment = TextAlignment.Center
        };
        dc.DrawText(text, new Point(40, Math.Max(40, (ActualHeight - text.Height) / 2)));
    }

    private readonly record struct LayoutMetrics(
        double ContentWidth,
        double ContentHeight,
        double FirstWidth,
        double FirstHeight,
        double SecondWidth,
        double SecondHeight,
        double GapScaled);
}
