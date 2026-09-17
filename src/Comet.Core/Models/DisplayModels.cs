namespace Comet.Core.Models;

public enum FitMode
{
    BestFit,
    FitWidth,
    FitHeight,
    Manual
}

public enum ReadingDirection
{
    LeftToRight,
    RightToLeft
}

public enum PageLayoutMode
{
    SinglePage,
    DoublePage
}

public enum BookOpenReason
{
    Explicit,
    StartupArgument,
    AdjacentNext,
    AdjacentPrevious
}

public readonly record struct PixelSize(int Width, int Height)
{
    public bool IsValid => Width > 0 && Height > 0;
    public bool IsLandscape => IsValid && Width > Height;
}

public readonly record struct ViewportSize(double Width, double Height)
{
    public bool IsValid => Width > 0 && Height > 0;
}
