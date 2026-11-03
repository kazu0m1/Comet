namespace Comet.Core.Models;

public static class AppSettingsNormalizer
{
    public const int CurrentSchemaVersion = 3;

    public static AppSettings Normalize(AppSettings? settings)
    {
        var value = settings ?? new AppSettings();

        return value with
        {
            SettingsSchemaVersion = CurrentSchemaVersion,
            FitMode = Enum.IsDefined(value.FitMode) ? value.FitMode : FitMode.BestFit,
            ReadingDirection = Enum.IsDefined(value.ReadingDirection)
                ? value.ReadingDirection
                : ReadingDirection.RightToLeft,
            PageLayoutMode = Enum.IsDefined(value.PageLayoutMode)
                ? value.PageLayoutMode
                : PageLayoutMode.DoublePage,
            FixedSizeDip = NormalizeFinite(value.FixedSizeDip, 1400, 64, 10000),
            FixedSizeAxis = Enum.IsDefined(value.FixedSizeAxis) ? value.FixedSizeAxis : FixedSizeAxis.Height,
            PdfFitMode = Enum.IsDefined(value.PdfFitMode) ? value.PdfFitMode : FitMode.BestFit,
            PdfReadingDirection = Enum.IsDefined(value.PdfReadingDirection) ? value.PdfReadingDirection : ReadingDirection.LeftToRight,
            PdfPageLayoutMode = Enum.IsDefined(value.PdfPageLayoutMode) ? value.PdfPageLayoutMode : PageLayoutMode.DoublePage,
            ThumbnailWidth = NormalizeFinite(value.ThumbnailWidth, 72, 48, 200),
            SmartScrollFraction = NormalizeFinite(value.SmartScrollFraction, 0.78, 0.10, 1.0),
            ArrowScrollPixels = NormalizeFinite(value.ArrowScrollPixels, 70, 10, 500)
        };
    }

    private static double NormalizeFinite(double value, double fallback, double minimum, double maximum)
        => double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
}
