using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Comet.Core.Models;
using Comet.Core.Services;

namespace Comet.App.Services;

// Light-weight placeholder for a virtualized PDF spread. The images are only
// loaded while the WPF item container is realized near the viewport.
public sealed class PdfContinuousRow : INotifyPropertyChanged
{
    private BitmapSource? _firstImage;
    private BitmapSource? _secondImage;
    private double _firstWidth, _firstHeight, _secondWidth, _secondHeight;

    public PdfContinuousRow(int firstPage, int? secondPage,
        ReadingDirection direction = ReadingDirection.LeftToRight)
    {
        FirstPage = firstPage;
        SecondPage = secondPage;
        // Keep page indices in document order; only reverse the visual columns.
        (FirstColumn, SecondColumn) = SpreadPlanner.DisplayColumns(direction, secondPage.HasValue);
    }
    public int FirstColumn { get; }
    public int SecondColumn { get; }
    public ObservableCollection<Rect> FirstHighlights { get; } = new();
    public ObservableCollection<Rect> SecondHighlights { get; } = new();
    public int FirstPage { get; }
    public int? SecondPage { get; }
    public bool IsLoading { get; set; }
    public bool Realized { get; set; }
    // Virtualization can re-realize a row while its previous async load is in flight.
    // Queue a new load after the in-flight operation rather than losing the request.
    public bool ReloadAfterCurrentLoad { get; set; }
    public BitmapSource? FirstImage { get => _firstImage; set { _firstImage = value; Notify(); } }
    public BitmapSource? SecondImage { get => _secondImage; set { _secondImage = value; Notify(); } }
    public double FirstWidth { get => _firstWidth; set { _firstWidth = value; Notify(); } }
    public double FirstHeight { get => _firstHeight; set { _firstHeight = value; Notify(); } }
    public double SecondWidth { get => _secondWidth; set { _secondWidth = value; Notify(); } }
    public double SecondHeight { get => _secondHeight; set { _secondHeight = value; Notify(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
