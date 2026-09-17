using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Comet.App.Services;
using Comet.Core.Abstractions;
using Comet.Core.Models;
using Comet.Core.Services;
using Comet.Infrastructure.Navigation;
using Comet.Platform.Windows.Imaging;

namespace Comet.App;

public partial class MainWindow : Window
{
    private readonly IBookSourceFactory _sourceFactory;
    private readonly ISettingsStore _settingsStore;
    private readonly IBookStateStore _stateStore;
    private readonly AdjacentArchiveFinder _archiveFinder;
    private readonly WpfBitmapDecoder _decoder = new();
    private readonly Localizer _text = new();
    private readonly ZoomSessionState _zoom;

    private IBookSource? _book;
    private PageImageCache? _imageCache;
    private AppSettings _settings;
    private List<Bookmark> _bookmarks = new();
    private readonly Stack<int> _navigationHistory = new();
    private int _pageIndex;
    private int _displayedPageCount = 1;
    private CancellationTokenSource? _openCts;
    private CancellationTokenSource? _renderCts;
    private CancellationTokenSource? _stateSaveCts;
    private long _renderGeneration;
    private bool _fullscreen;
    private bool _uiHidden;
    private bool _isClosing;

    public MainWindow(
        IBookSourceFactory sourceFactory,
        ISettingsStore settingsStore,
        IBookStateStore stateStore,
        AdjacentArchiveFinder archiveFinder,
        AppSettings settings)
    {
        _sourceFactory = sourceFactory;
        _settingsStore = settingsStore;
        _stateStore = stateStore;
        _archiveFinder = archiveFinder;
        _settings = settings;
        _zoom = new ZoomSessionState(settings.FitMode);

        InitializeComponent();
        LocalizeUi();
        ApplySettingsToViewport();
        UpdateMenuChecks();
        UpdateZoomText();
        StatusText.Text = _text["Ready"];
    }

    public async Task OpenPathAsync(string path, BookOpenReason reason = BookOpenReason.Explicit)
    {
        _openCts?.Cancel();
        _openCts?.Dispose();
        _openCts = new CancellationTokenSource();
        var cancellationToken = _openCts.Token;

        try
        {
            StatusText.Text = _text["Opening"];
            await CloseCurrentBookAsync(saveState: true).ConfigureAwait(true);

            if (reason is BookOpenReason.Explicit)
            {
                _zoom.ResetTemporaryZoom();
                Viewport.TemporaryZoomFactor = _zoom.TemporaryZoomFactor;
            }
            else if (reason is BookOpenReason.AdjacentNext or BookOpenReason.AdjacentPrevious)
            {
                _zoom.OnAdjacentArchiveOpened();
            }

            var requestedPath = Path.GetFullPath(path);
            var source = await _sourceFactory.OpenAsync(requestedPath, cancellationToken).ConfigureAwait(true);
            if (cancellationToken.IsCancellationRequested)
            {
                await source.DisposeAsync().ConfigureAwait(true);
                cancellationToken.ThrowIfCancellationRequested();
            }
            _book = source;
            _imageCache = new PageImageCache(source, _decoder);
            _navigationHistory.Clear();
            Title = $"Comet — {source.Descriptor.DisplayName}";

            var savedState = await _stateStore.LoadAsync(source.Descriptor.Path, cancellationToken).ConfigureAwait(true);
            _bookmarks = savedState?.Bookmarks?.ToList() ?? new List<Bookmark>();

            if (source.Descriptor.Pages.Count == 0)
            {
                _pageIndex = 0;
                Viewport.FirstPage = null;
                Viewport.SecondPage = null;
                Viewport.ErrorMessage = _text["NoImages"];
                Viewport.Refresh();
                StatusText.Text = _text["NoImages"];
                return;
            }

            _pageIndex = reason switch
            {
                BookOpenReason.AdjacentNext => 0,
                BookOpenReason.AdjacentPrevious => Math.Max(0, source.Descriptor.Pages.Count - 1),
                _ => Math.Clamp(savedState?.LastPageIndex ?? 0, 0, source.Descriptor.Pages.Count - 1)
            };

            if (File.Exists(requestedPath) && Comet.Infrastructure.Sources.SupportedImages.IsSupported(requestedPath))
            {
                var requestedName = Path.GetFileName(requestedPath);
                var index = source.Descriptor.Pages.ToList().FindIndex(p => string.Equals(p.Name, requestedName, StringComparison.OrdinalIgnoreCase));
                if (index >= 0) _pageIndex = index;
            }

            if (reason == BookOpenReason.AdjacentPrevious && _settings.PageLayoutMode == PageLayoutMode.DoublePage)
                _pageIndex = SpreadPlanner.NormalizeStartIndex(_pageIndex, source.Descriptor.Pages.Count, _settings.PageLayoutMode);

            await RenderCurrentAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Superseded by another open request.
        }
        catch (Exception ex)
        {
            Viewport.FirstPage = null;
            Viewport.SecondPage = null;
            Viewport.ErrorMessage = ex.Message;
            Viewport.Refresh();
            StatusText.Text = $"{_text["Error"]}: {ex.Message}";
        }
    }

    private async Task RenderCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (_book is null || _imageCache is null || _book.Descriptor.Pages.Count == 0)
            return;

        _pageIndex = Math.Clamp(_pageIndex, 0, _book.Descriptor.Pages.Count - 1);
        var renderPageIndex = _pageIndex;
        var generation = Interlocked.Increment(ref _renderGeneration);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previous = _renderCts;
        _renderCts = cts;
        previous?.Cancel();
        // The superseded render owns and disposes its CTS in its own finally block.
        var token = cts.Token;

        try
        {
            var targetWidth = GetDecodeTargetWidth(renderPageIndex);
            var first = await _imageCache.GetAsync(renderPageIndex, targetWidth, token).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            if (generation != Volatile.Read(ref _renderGeneration)) return;

            Viewport.ErrorMessage = null;
            Viewport.FirstPage = first;
            Viewport.SecondPage = null;
            _displayedPageCount = 1;

            if (_settings.PageLayoutMode == PageLayoutMode.DoublePage
                && renderPageIndex > 0
                && renderPageIndex + 1 < _book.Descriptor.Pages.Count
                && first.PixelWidth <= first.PixelHeight)
            {
                try
                {
                    var second = await _imageCache.GetAsync(renderPageIndex + 1, targetWidth, token).ConfigureAwait(true);
                    token.ThrowIfCancellationRequested();
                    if (generation != Volatile.Read(ref _renderGeneration)) return;
                    if (second.PixelWidth <= second.PixelHeight)
                    {
                        Viewport.SecondPage = second;
                        _displayedPageCount = 2;
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception) when (!token.IsCancellationRequested)
                {
                    Viewport.SecondPage = null;
                    _displayedPageCount = 1;
                }
            }

            token.ThrowIfCancellationRequested();
            if (generation != Volatile.Read(ref _renderGeneration)) return;
            Viewport.TemporaryZoomFactor = _zoom.TemporaryZoomFactor;
            Viewport.ResetScroll();
            UpdateStatus();
            PrefetchNeighbors(targetWidth, renderPageIndex, _displayedPageCount);
            ScheduleStateSave();
        }
        catch (OperationCanceledException)
        {
            // A newer render/open/close request superseded this render.
        }
        catch (Exception ex)
        {
            if (generation != Volatile.Read(ref _renderGeneration)) return;
            Viewport.FirstPage = null;
            Viewport.SecondPage = null;
            Viewport.ErrorMessage = $"{_text["Error"]}: {ex.Message}";
            Viewport.Refresh();
            StatusText.Text = Viewport.ErrorMessage;
            _displayedPageCount = 1;
        }
        finally
        {
            if (ReferenceEquals(_renderCts, cts))
                _renderCts = null;
            cts.Dispose();
        }
    }

    private int GetDecodeTargetWidth(int pageIndex)
    {
        if (_settings.FitMode == FitMode.Manual)
            return 0;

        var dpi = VisualTreeHelper.GetDpi(Viewport).DpiScaleX;
        var width = Viewport.ActualWidth > 1 ? Viewport.ActualWidth : 1600;
        if (_settings.PageLayoutMode == PageLayoutMode.DoublePage && pageIndex > 0)
            width *= 0.58;
        var requested = width * dpi * 1.35 * Math.Max(1.0, _zoom.TemporaryZoomFactor);
        return (int)Math.Clamp(Math.Ceiling(requested), 1200, 4096);
    }

    private void PrefetchNeighbors(int targetWidth, int pageIndex, int displayedPageCount)
    {
        if (_book is null || _imageCache is null) return;
        var next = pageIndex + displayedPageCount;
        var pages = new[] { next, next + 1, pageIndex - 1, pageIndex - 2 };
        _imageCache.Prefetch(pages, targetWidth);
    }

    private async Task NextAsync(bool onePage = false)
    {
        if (_book is null) return;
        var step = onePage ? 1 : Math.Max(1, _displayedPageCount);
        if (_pageIndex + step >= _book.Descriptor.Pages.Count) return;
        _navigationHistory.Push(_pageIndex);
        _pageIndex += step;
        await RenderCurrentAsync();
    }

    private async Task PreviousAsync(bool onePage = false)
    {
        if (_book is null || _pageIndex <= 0) return;
        if (onePage)
        {
            _pageIndex = Math.Max(0, _pageIndex - 1);
        }
        else if (_navigationHistory.Count > 0)
        {
            _pageIndex = _navigationHistory.Pop();
        }
        else
        {
            _pageIndex = SpreadPlanner.PreviousStartIndex(_pageIndex, _settings.PageLayoutMode);
        }
        await RenderCurrentAsync();
    }

    private async Task GoToPageAsync(int pageIndex)
    {
        if (_book is null || _book.Descriptor.Pages.Count == 0) return;
        _pageIndex = Math.Clamp(pageIndex, 0, _book.Descriptor.Pages.Count - 1);
        _navigationHistory.Clear();
        await RenderCurrentAsync();
    }

    private async Task OpenAdjacentArchiveAsync(bool next)
    {
        if (_book is null) return;
        var path = next
            ? _archiveFinder.FindNext(_book.Descriptor.Path)
            : _archiveFinder.FindPrevious(_book.Descriptor.Path);
        if (path is null) return;
        await OpenPathAsync(path, next ? BookOpenReason.AdjacentNext : BookOpenReason.AdjacentPrevious);
    }

    private bool SmartScroll(int direction)
    {
        return Viewport.SmartScroll(direction, _settings.SmartScrollFraction);
    }

    private async Task SmartScrollOrFlipAsync(int direction)
    {
        if (SmartScroll(direction)) return;
        if (!_settings.FlipPageAtScrollEdge) return;
        if (direction > 0) await NextAsync(); else await PreviousAsync();
    }

    private void SetFit(FitMode mode)
    {
        _zoom.SetBaseFitMode(mode);
        _settings = _settings with { FitMode = mode };
        Viewport.FitMode = mode;
        Viewport.TemporaryZoomFactor = _zoom.TemporaryZoomFactor;
        Viewport.ResetScroll();
        UpdateMenuChecks();
        UpdateZoomText();
        _ = SaveSettingsSafeAsync();
        if (_book is not null)
            _ = RenderCurrentAsync();
    }

    private void AdjustZoom(double factor)
    {
        _zoom.AdjustTemporaryZoom(factor);
        Viewport.TemporaryZoomFactor = _zoom.TemporaryZoomFactor;
        Viewport.Refresh();
        UpdateZoomText();
    }

    private void ResetTemporaryZoom()
    {
        _zoom.ResetTemporaryZoom();
        Viewport.TemporaryZoomFactor = 1.0;
        Viewport.ResetScroll();
        UpdateZoomText();
    }

    private void ToggleManga()
    {
        var direction = _settings.ReadingDirection == ReadingDirection.RightToLeft
            ? ReadingDirection.LeftToRight
            : ReadingDirection.RightToLeft;
        _settings = _settings with { ReadingDirection = direction };
        Viewport.ReadingDirection = direction;
        Viewport.ResetScroll();
        UpdateMenuChecks();
        _ = SaveSettingsSafeAsync();
    }

    private async Task ToggleDoublePageAsync()
    {
        var mode = _settings.PageLayoutMode == PageLayoutMode.DoublePage
            ? PageLayoutMode.SinglePage
            : PageLayoutMode.DoublePage;
        _settings = _settings with { PageLayoutMode = mode };
        if (_book is not null && mode == PageLayoutMode.DoublePage)
            _pageIndex = SpreadPlanner.NormalizeStartIndex(_pageIndex, _book.Descriptor.Pages.Count, mode);
        UpdateMenuChecks();
        await SaveSettingsSafeAsync();
        await RenderCurrentAsync();
    }

    private void ToggleStretch()
    {
        _settings = _settings with { StretchSmallImages = !_settings.StretchSmallImages };
        Viewport.StretchSmallImages = _settings.StretchSmallImages;
        Viewport.ResetScroll();
        UpdateMenuChecks();
        _ = SaveSettingsSafeAsync();
    }

    private void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        if (_fullscreen)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
        }
        ApplyChromeVisibility();
    }

    private void ToggleUiHidden()
    {
        _uiHidden = !_uiHidden;
        ApplyChromeVisibility();
    }

    private void ApplyChromeVisibility()
    {
        var visibility = (_fullscreen || _uiHidden) ? Visibility.Collapsed : Visibility.Visible;
        MainMenu.Visibility = visibility;
        MainToolbarTray.Visibility = visibility;
        MainStatusBar.Visibility = visibility;
    }

    private void ApplySettingsToViewport()
    {
        Viewport.FitMode = _settings.FitMode;
        Viewport.ReadingDirection = _settings.ReadingDirection;
        Viewport.StretchSmallImages = _settings.StretchSmallImages;
        Viewport.TemporaryZoomFactor = _zoom.TemporaryZoomFactor;
    }

    private void UpdateMenuChecks()
    {
        BestFitMenuItem.IsChecked = _settings.FitMode == FitMode.BestFit;
        FitWidthMenuItem.IsChecked = _settings.FitMode == FitMode.FitWidth;
        FitHeightMenuItem.IsChecked = _settings.FitMode == FitMode.FitHeight;
        ManualMenuItem.IsChecked = _settings.FitMode == FitMode.Manual;
        DoublePageMenuItem.IsChecked = _settings.PageLayoutMode == PageLayoutMode.DoublePage;
        MangaModeMenuItem.IsChecked = _settings.ReadingDirection == ReadingDirection.RightToLeft;
        StretchMenuItem.IsChecked = _settings.StretchSmallImages;
    }

    private void LocalizeUi()
    {
        FileMenu.Header = _text["File"];
        OpenMenuItem.Header = _text["Open"];
        OpenFolderMenuItem.Header = _text["OpenFolder"];
        ExitMenuItem.Header = _text["Exit"];
        NavigateMenu.Header = _text["Navigate"];
        PreviousPageMenuItem.Header = _text["PreviousPage"];
        NextPageMenuItem.Header = _text["NextPage"];
        PreviousArchiveMenuItem.Header = _text["PreviousArchive"];
        NextArchiveMenuItem.Header = _text["NextArchive"];
        ViewMenu.Header = _text["View"];
        BestFitMenuItem.Header = _text["BestFit"];
        FitWidthMenuItem.Header = _text["FitWidth"];
        FitHeightMenuItem.Header = _text["FitHeight"];
        ManualMenuItem.Header = _text["Manual"];
        DoublePageMenuItem.Header = _text["DoublePage"];
        MangaModeMenuItem.Header = _text["MangaMode"];
        StretchMenuItem.Header = _text["Stretch"];
        FullscreenMenuItem.Header = _text["Fullscreen"];
        BookmarksMenu.Header = _text["Bookmarks"];
        AddBookmarkMenuItem.Header = _text["AddBookmark"];
        EditBookmarksMenuItem.Header = _text["EditBookmarks"];
    }

    private void UpdateStatus()
    {
        if (_book is null) return;
        var end = Math.Min(_book.Descriptor.Pages.Count, _pageIndex + _displayedPageCount);
        StatusText.Text = _displayedPageCount > 1
            ? $"{_pageIndex + 1}-{end} / {_book.Descriptor.Pages.Count}"
            : $"{_pageIndex + 1} / {_book.Descriptor.Pages.Count}";
        UpdateZoomText();
    }

    private void UpdateZoomText() => ZoomText.Text = $"{_text["Zoom"]}: {_zoom.TemporaryZoomFactor:P0}";

    private void AddBookmark()
    {
        if (_book is null) return;
        if (_bookmarks.All(b => b.PageIndex != _pageIndex))
            _bookmarks.Add(new Bookmark(_pageIndex, DateTimeOffset.Now));
        _bookmarks = _bookmarks.OrderBy(b => b.PageIndex).ToList();
        ScheduleStateSave();
        StatusText.Text = $"{_text["BookmarkAdded"]}: {_pageIndex + 1}";
    }

    private async Task ShowBookmarksAsync()
    {
        if (_book is null) return;
        var dialog = new BookmarksWindow(_bookmarks, _text) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedPageIndex is int page)
            await GoToPageAsync(page);
    }

    private void ScheduleStateSave()
    {
        if (_book is null || _isClosing) return;
        _stateSaveCts?.Cancel();
        _stateSaveCts?.Dispose();
        _stateSaveCts = new CancellationTokenSource();
        var token = _stateSaveCts.Token;
        var path = _book.Descriptor.Path;
        var state = BuildCurrentBookState();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token).ConfigureAwait(false);
                await _stateStore.SaveAsync(path, state, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch { }
        }, CancellationToken.None);
    }

    private BookState BuildCurrentBookState()
    {
        if (_book is null) return BookState.Empty;
        var path = _book.Descriptor.Path;
        long length = 0;
        DateTimeOffset modified = DateTimeOffset.MinValue;
        try
        {
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                length = info.Length;
                modified = info.LastWriteTimeUtc;
            }
            else if (Directory.Exists(path))
            {
                modified = new DirectoryInfo(path).LastWriteTimeUtc;
            }
        }
        catch (IOException) { }
        return new BookState(_pageIndex, _bookmarks.ToArray(), length, modified);
    }

    private async Task SaveCurrentStateNowAsync()
    {
        if (_book is null) return;
        _stateSaveCts?.Cancel();
        var path = _book.Descriptor.Path;
        var state = BuildCurrentBookState();
        try { await _stateStore.SaveAsync(path, state).ConfigureAwait(false); } catch { }
    }

    private async Task SaveSettingsSafeAsync()
    {
        try { await _settingsStore.SaveAsync(_settings).ConfigureAwait(false); } catch { }
    }

    private async Task CloseCurrentBookAsync(bool saveState)
    {
        _renderCts?.Cancel();
        Interlocked.Increment(ref _renderGeneration);
        if (_book is null) return;
        if (saveState) await SaveCurrentStateNowAsync().ConfigureAwait(true);
        _imageCache?.Dispose();
        _imageCache = null;
        await _book.DisposeAsync().ConfigureAwait(true);
        _book = null;
        _bookmarks.Clear();
        Viewport.FirstPage = null;
        Viewport.SecondPage = null;
        Viewport.ErrorMessage = null;
        Viewport.Refresh();
        Title = "Comet";
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _isClosing = true;
        _openCts?.Cancel();
        _renderCts?.Cancel();
        Interlocked.Increment(ref _renderGeneration);
        _stateSaveCts?.Cancel();
        try { SaveCurrentStateNowAsync().GetAwaiter().GetResult(); } catch { }
        _imageCache?.Dispose();
        if (_book is not null)
        {
            try { _book.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
        }
        _openCts?.Dispose();
        _stateSaveCts?.Dispose();
        base.OnClosing(e);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var modifiers = Keyboard.Modifiers;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (modifiers == ModifierKeys.Control && key == Key.O) { Open_Click(sender, e); e.Handled = true; return; }
        if (modifiers == ModifierKeys.Control && key == Key.Q) { Close(); e.Handled = true; return; }
        if (modifiers == ModifierKeys.Control && key == Key.W) { await CloseCurrentBookAsync(true); StatusText.Text = _text["Ready"]; e.Handled = true; return; }
        if (modifiers == ModifierKeys.Control && key == Key.D) { AddBookmark(); e.Handled = true; return; }
        if (modifiers == ModifierKeys.Control && key == Key.B) { await ShowBookmarksAsync(); e.Handled = true; return; }
        if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.N) { await OpenAdjacentArchiveAsync(true); e.Handled = true; return; }
        if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.P) { await OpenAdjacentArchiveAsync(false); e.Handled = true; return; }
        if (modifiers == ModifierKeys.Control && (key == Key.D0 || key == Key.NumPad0)) { ResetTemporaryZoom(); e.Handled = true; return; }

        if (modifiers.HasFlag(ModifierKeys.Alt) && (key == Key.Left || key == Key.Right))
        {
            var manga = _settings.ReadingDirection == ReadingDirection.RightToLeft;
            var next = manga ? key == Key.Left : key == Key.Right;
            if (next) await NextAsync(onePage: true); else await PreviousAsync(onePage: true);
            e.Handled = true;
            return;
        }

        if (key == Key.PageDown)
        {
            var one = modifiers.HasFlag(ModifierKeys.Control);
            if (modifiers.HasFlag(ModifierKeys.Shift) && one) await PreviousAsync(onePage: true); else await NextAsync(one);
            e.Handled = true;
            return;
        }
        if (key == Key.PageUp || key == Key.Back) { await PreviousAsync(); e.Handled = true; return; }
        if (key == Key.Home) { await GoToPageAsync(0); e.Handled = true; return; }
        if (key == Key.End && _book is not null) { await GoToPageAsync(_book.Descriptor.Pages.Count - 1); e.Handled = true; return; }

        if (key == Key.Space)
        {
            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                if (modifiers.HasFlag(ModifierKeys.Shift)) await PreviousAsync(onePage: true); else await NextAsync(onePage: true);
            }
            else
            {
                await SmartScrollOrFlipAsync(modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1);
            }
            e.Handled = true;
            return;
        }

        if (key == Key.Down) { Viewport.ScrollBy(0, _settings.ArrowScrollPixels); e.Handled = true; return; }
        if (key == Key.Up) { Viewport.ScrollBy(0, -_settings.ArrowScrollPixels); e.Handled = true; return; }
        if (key == Key.Left) { Viewport.ScrollBy(-_settings.ArrowScrollPixels, 0); e.Handled = true; return; }
        if (key == Key.Right) { Viewport.ScrollBy(_settings.ArrowScrollPixels, 0); e.Handled = true; return; }

        if (key == Key.B) { SetFit(FitMode.BestFit); e.Handled = true; return; }
        if (key == Key.W) { SetFit(FitMode.FitWidth); e.Handled = true; return; }
        if (key == Key.H) { SetFit(FitMode.FitHeight); e.Handled = true; return; }
        if (key == Key.A) { SetFit(FitMode.Manual); e.Handled = true; return; }
        if (key == Key.M) { ToggleManga(); e.Handled = true; return; }
        if (key == Key.D) { await ToggleDoublePageAsync(); e.Handled = true; return; }
        if (key == Key.Y) { ToggleStretch(); e.Handled = true; return; }
        if (key == Key.I) { ToggleUiHidden(); e.Handled = true; return; }
        if (key is Key.F or Key.F11) { ToggleFullscreen(); e.Handled = true; return; }
        if (key is Key.OemPlus or Key.Add) { AdjustZoom(1.10); e.Handled = true; return; }
        if (key is Key.OemMinus or Key.Subtract) { AdjustZoom(1 / 1.10); e.Handled = true; }
    }

    private async void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            AdjustZoom(e.Delta > 0 ? 1.10 : 1 / 1.10);
            e.Handled = true;
            return;
        }

        await SmartScrollOrFlipAsync(e.Delta < 0 ? 1 : -1);
        e.Handled = true;
    }

    private async void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            await NextAsync();
            e.Handled = true;
        }
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0) return;
        await OpenPathAsync(paths[0], BookOpenReason.Explicit);
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = _text["OpenComic"],
            Filter = "Comic archives and images (*.zip;*.cbz;*.jpg;*.jpeg;*.png)|*.zip;*.cbz;*.jpg;*.jpeg;*.png|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == true)
            await OpenPathAsync(dialog.FileName, BookOpenReason.Explicit);
    }

    private async void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog(this) == true)
            await OpenPathAsync(dialog.FolderName, BookOpenReason.Explicit);
    }

    private async void CloseBook_Click(object sender, RoutedEventArgs e) { await CloseCurrentBookAsync(true); StatusText.Text = _text["Ready"]; }
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
    private async void Previous_Click(object sender, RoutedEventArgs e) => await PreviousAsync();
    private async void Next_Click(object sender, RoutedEventArgs e) => await NextAsync();
    private async void PreviousArchive_Click(object sender, RoutedEventArgs e) => await OpenAdjacentArchiveAsync(false);
    private async void NextArchive_Click(object sender, RoutedEventArgs e) => await OpenAdjacentArchiveAsync(true);
    private void BestFit_Click(object sender, RoutedEventArgs e) => SetFit(FitMode.BestFit);
    private void FitWidth_Click(object sender, RoutedEventArgs e) => SetFit(FitMode.FitWidth);
    private void FitHeight_Click(object sender, RoutedEventArgs e) => SetFit(FitMode.FitHeight);
    private void Manual_Click(object sender, RoutedEventArgs e) => SetFit(FitMode.Manual);
    private async void DoublePage_Click(object sender, RoutedEventArgs e) => await ToggleDoublePageAsync();
    private void MangaMode_Click(object sender, RoutedEventArgs e) => ToggleManga();
    private void Stretch_Click(object sender, RoutedEventArgs e) => ToggleStretch();
    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();
    private void AddBookmark_Click(object sender, RoutedEventArgs e) => AddBookmark();
    private async void EditBookmarks_Click(object sender, RoutedEventArgs e) => await ShowBookmarksAsync();
}
