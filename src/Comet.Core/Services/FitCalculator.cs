using Comet.Core.Models;

namespace Comet.Core.Services;

public static class FitCalculator
{
    public static double CalculateScale(
        PixelSize content,
        ViewportSize viewport,
        FitMode fitMode,
        bool stretchSmallImages,
        double manualScale = 1.0)
    {
        if (!content.IsValid || !viewport.IsValid)
            return 1.0;

        var sx = viewport.Width / content.Width;
        var sy = viewport.Height / content.Height;

        var scale = fitMode switch
        {
            FitMode.BestFit => Math.Min(sx, sy),
            FitMode.FitWidth => sx,
            FitMode.FitHeight => sy,
            FitMode.Manual => manualScale,
            FitMode.FixedSize => manualScale,
            _ => 1.0
        };

        if (!stretchSmallImages && fitMode is not FitMode.Manual)
            scale = Math.Min(scale, 1.0);

        return Math.Clamp(scale, 0.01, 32.0);
    }

    public static double FixedPageScale(PixelSize page, FixedSizeAxis axis, double targetDip, bool stretchSmallImages)
    {
        if (!page.IsValid || !double.IsFinite(targetDip)) return 1.0;
        var scale = Math.Clamp(targetDip / (axis == FixedSizeAxis.Width ? page.Width : page.Height), 0.01, 32.0);
        return stretchSmallImages ? scale : Math.Min(scale, 1.0);
    }

    public static PixelSize CombineSpread(PixelSize first, PixelSize? second, int gapPixels = 12)
    {
        if (second is null || !second.Value.IsValid)
            return first;

        return new PixelSize(
            checked(first.Width + second.Value.Width + gapPixels),
            Math.Max(first.Height, second.Value.Height));
    }
}
