namespace Comet.Core.Services;

public sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var ix = 0;
        var iy = 0;
        while (ix < x.Length && iy < y.Length)
        {
            if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
            {
                var runStartX = ix;
                var runStartY = iy;
                while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                while (iy < y.Length && char.IsDigit(y[iy])) iy++;

                var sigX = runStartX;
                var sigY = runStartY;
                while (sigX < ix && x[sigX] == '0') sigX++;
                while (sigY < iy && y[sigY] == '0') sigY++;

                var digitsX = ix - sigX;
                var digitsY = iy - sigY;
                if (digitsX != digitsY) return digitsX.CompareTo(digitsY);

                for (var i = 0; i < digitsX; i++)
                {
                    var cmp = x[sigX + i].CompareTo(y[sigY + i]);
                    if (cmp != 0) return cmp;
                }

                var runLengthX = ix - runStartX;
                var runLengthY = iy - runStartY;
                if (runLengthX != runLengthY) return runLengthX.CompareTo(runLengthY);
                continue;
            }

            var cx = char.ToUpperInvariant(x[ix]);
            var cy = char.ToUpperInvariant(y[iy]);
            var charCompare = cx.CompareTo(cy);
            if (charCompare != 0) return charCompare;
            ix++;
            iy++;
        }

        return (x.Length - ix).CompareTo(y.Length - iy);
    }
}
