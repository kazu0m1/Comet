using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Comet.App.Services;

public sealed class ThumbnailItem : INotifyPropertyChanged
{
    private BitmapSource? _image;
    private bool _isLoading;

    public ThumbnailItem(int pageIndex, string pageName)
    {
        PageIndex = pageIndex;
        PageName = pageName;
        PageLabel = $"{pageIndex + 1}";
    }

    public int PageIndex { get; }
    public string PageName { get; }
    public string PageLabel { get; }

    public BitmapSource? Image
    {
        get => _image;
        set
        {
            if (ReferenceEquals(_image, value)) return;
            _image = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
