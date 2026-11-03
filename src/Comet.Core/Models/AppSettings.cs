namespace Comet.Core.Models;

public sealed record AppSettings
{
    public int SettingsSchemaVersion { get; init; } = 3;
    public double FixedSizeDip { get; init; } = 1400;
    public FixedSizeAxis FixedSizeAxis { get; init; } = FixedSizeAxis.Height;
    public FitMode PdfFitMode { get; init; } = FitMode.BestFit;
    public ReadingDirection PdfReadingDirection { get; init; } = ReadingDirection.LeftToRight;
    public PageLayoutMode PdfPageLayoutMode { get; init; } = PageLayoutMode.DoublePage;
    public FitMode FitMode { get; init; } = FitMode.BestFit;
    public ReadingDirection ReadingDirection { get; init; } = ReadingDirection.RightToLeft;
    public PageLayoutMode PageLayoutMode { get; init; } = PageLayoutMode.DoublePage;
    public bool StretchSmallImages { get; init; } = true;
    public bool ShowThumbnails { get; init; } = true;
    public double ThumbnailWidth { get; init; } = 72;
    public bool FlipPageAtScrollEdge { get; init; } = true;
    public double SmartScrollFraction { get; init; } = 0.78;
    public double ArrowScrollPixels { get; init; } = 70;
}
