namespace Comet.Core.Models;

public sealed record AppSettings
{
    public int SettingsSchemaVersion { get; init; } = 1;
    public FitMode FitMode { get; init; } = FitMode.BestFit;
    public ReadingDirection ReadingDirection { get; init; } = ReadingDirection.RightToLeft;
    public PageLayoutMode PageLayoutMode { get; init; } = PageLayoutMode.DoublePage;
    public bool StretchSmallImages { get; init; } = true;
    public bool ShowThumbnails { get; init; } = true;
    public double ThumbnailWidth { get; init; } = 180;
    public bool FlipPageAtScrollEdge { get; init; } = true;
    public double SmartScrollFraction { get; init; } = 0.78;
    public double ArrowScrollPixels { get; init; } = 70;
}
