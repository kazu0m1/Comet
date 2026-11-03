namespace Comet.Core.Models;

// Normalized bounds of the retained image within the fully rendered page.
public readonly record struct PdfCropBounds(double Left, double Top, double Right, double Bottom)
{
    public static PdfCropBounds Full => new(0, 0, 1, 1);
    public (double U, double V) ToOriginal(double u, double v)
        => (Left + u * (Right - Left), Top + v * (Bottom - Top));
    public (double U, double V) FromOriginal(double u, double v)
        => ((u - Left) / Math.Max(0.000001, Right - Left),
            (v - Top) / Math.Max(0.000001, Bottom - Top));
}
