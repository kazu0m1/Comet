namespace Comet.Core.Services;

// PDF coordinates use a bottom-left origin. Display coordinates use a top-left origin.
// Quarter turns are clockwise, consistent with PDFium FPDF_RenderPageBitmap.
public static class PdfCoordinateMapper
{
    public static (double X, double Y) DisplayToPdf(double u, double v, int turns)
        => (((turns % 4) + 4) % 4) switch
        {
            1 => (v, u),
            2 => (1 - u, v),
            3 => (1 - v, 1 - u),
            _ => (u, 1 - v)
        };

    public static (double U, double V) PdfToDisplay(double x, double y, int turns)
        => (((turns % 4) + 4) % 4) switch
        {
            1 => (y, x),
            2 => (1 - x, y),
            3 => (1 - y, 1 - x),
            _ => (x, 1 - y)
        };
}
