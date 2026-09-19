namespace Comet.Core.Models;

public static class AppSettingsNormalizer
{
    public const int CurrentSchemaVersion = 2;

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
            ThumbnailWidth = NormalizeFinite(value.ThumbnailWidth, 72, 48, 200),
            SmartScrollFraction = NormalizeFinite(value.SmartScrollFraction, 0.78, 0.10, 1.0),
            ArrowScrollPixels = NormalizeFinite(value.ArrowScrollPixels, 70, 10, 500)
        };
    }

    private static double NormalizeFinite(double value, double fallback, double minimum, double maximum)
        => double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
}
