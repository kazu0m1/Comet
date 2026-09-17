using Comet.Core.Models;

namespace Comet.Core.Services;

public sealed class ZoomSessionState
{
    public ZoomSessionState(FitMode baseFitMode = FitMode.BestFit)
    {
        BaseFitMode = baseFitMode;
    }

    public FitMode BaseFitMode { get; private set; }
    public double TemporaryZoomFactor { get; private set; } = 1.0;

    public void SetBaseFitMode(FitMode mode)
    {
        BaseFitMode = mode;
        TemporaryZoomFactor = 1.0;
    }

    public void AdjustTemporaryZoom(double factor)
    {
        if (factor <= 0 || double.IsNaN(factor) || double.IsInfinity(factor))
            throw new ArgumentOutOfRangeException(nameof(factor));
        TemporaryZoomFactor = Math.Clamp(TemporaryZoomFactor * factor, 0.10, 8.0);
    }

    public void ResetTemporaryZoom() => TemporaryZoomFactor = 1.0;

    public void OnAdjacentArchiveOpened()
    {
        // Deliberately preserve the temporary zoom factor between adjacent archives.
    }

    public void ResetForNewProcess() => TemporaryZoomFactor = 1.0;
}
