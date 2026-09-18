namespace Comet.Core.Services;

public static class DpiScaleCalculator
{
    public static double ManualDipScale(double dpiScale)
        => 1.0 / Math.Max(0.01, dpiScale);

    public static double PhysicalZoom(double renderedWidthDip, int sourcePixelWidth, double dpiScale)
    {
        if (renderedWidthDip <= 0 || sourcePixelWidth <= 0 || dpiScale <= 0)
            return 1.0;

        return renderedWidthDip * dpiScale / sourcePixelWidth;
    }
}
