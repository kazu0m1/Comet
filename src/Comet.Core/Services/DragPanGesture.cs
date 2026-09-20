namespace Comet.Core.Services;

public sealed class DragPanGesture
{
    private double _startX;
    private double _startY;
    private double _lastX;
    private double _lastY;

    public bool IsTracking { get; private set; }
    public bool IsDragging { get; private set; }

    public void Begin(double x, double y)
    {
        _startX = x;
        _startY = y;
        _lastX = x;
        _lastY = y;
        IsTracking = true;
        IsDragging = false;
    }

    public DragPanMove Move(
        double x,
        double y,
        double minimumHorizontalDistance,
        double minimumVerticalDistance,
        bool canPan)
    {
        if (!IsTracking)
            return default;

        if (!IsDragging)
        {
            if (!canPan ||
                (Math.Abs(x - _startX) < minimumHorizontalDistance &&
                 Math.Abs(y - _startY) < minimumVerticalDistance))
            {
                return default;
            }

            IsDragging = true;
        }

        var move = new DragPanMove(true, x - _lastX, y - _lastY);
        _lastX = x;
        _lastY = y;
        return move;
    }

    public bool End()
    {
        var wasDragging = IsDragging;
        Reset();
        return wasDragging;
    }

    public void Cancel() => Reset();

    private void Reset()
    {
        IsTracking = false;
        IsDragging = false;
    }
}

public readonly record struct DragPanMove(bool IsDragging, double DeltaX, double DeltaY);
