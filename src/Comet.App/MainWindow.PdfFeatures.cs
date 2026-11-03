using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using Comet.Infrastructure.Sources;
using Comet.Core.Services;

namespace Comet.App;

public partial class MainWindow
{
    private Window? _pdfSearchWindow;
    private TextBox? _pdfQueryBox;
    private TextBlock? _pdfSearchStatus;
    private CancellationTokenSource? _pdfSearchCts;
    private readonly List<(int Page, int Start, int Length)> _pdfMatches = new();
    private int _pdfMatchCursor = -1;

    private async Task LoadPdfOutlineAsync(CancellationToken token)
    {
        PdfOutlineTree.Items.Clear();
        var pdf = _book as PdfBookSource;
        PdfOutlineTabButton.Visibility = pdf is null ? Visibility.Collapsed : Visibility.Visible;
        if (pdf is null)
        {
            ShowThumbnailTab();
            return;
        }

        try
        {
            var reply = await pdf.GetOutlineAsync(token).ConfigureAwait(true);
            if (!ReferenceEquals(_book, pdf) || token.IsCancellationRequested) return;
            if (reply.Outline is { Count: > 0 })
            {
                foreach (var node in reply.Outline) AddOutlineNode(PdfOutlineTree.Items, node);
            }
            else
                PdfOutlineTree.Items.Add(new TreeViewItem { Header = "No table of contents", IsEnabled = false });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ReferenceEquals(_book, pdf))
        {
            PdfOutlineTree.Items.Add(new TreeViewItem { Header = "Contents unavailable: " + ex.Message, IsEnabled = false });
        }
    }

    private static void AddOutlineNode(ItemCollection parent, PdfOutlineItem node)
    {
        var child = new TreeViewItem { Header = node.Title, Tag = node.PageIndex };
        parent.Add(child);
        foreach (var item in node.Children) AddOutlineNode(child.Items, item);
    }

    private void ShowThumbnailTab()
    {
        ThumbnailList.Visibility = Visibility.Visible;
        PdfOutlineTree.Visibility = Visibility.Collapsed;
    }
    private void PdfThumbnailTab_Click(object sender, RoutedEventArgs e) => ShowThumbnailTab();
    private void PdfOutlineTab_Click(object sender, RoutedEventArgs e)
    {
        if (_book is not PdfBookSource) return;
        ThumbnailList.Visibility = Visibility.Collapsed;
        PdfOutlineTree.Visibility = Visibility.Visible;
    }
    private async void PdfOutlineTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_book is not PdfBookSource) return;
        if (e.NewValue is TreeViewItem { Tag: int pageIndex } && pageIndex >= 0)
            await GoToPageAsync(pageIndex);
    }

    private void PdfSearch_Click(object sender, RoutedEventArgs e) => ShowPdfSearch();
    private void ShowPdfSearch()
    {
        if (_book is not PdfBookSource)
        {
            MessageBox.Show(this, "Text search is available for PDF documents.", "Comet");
            return;
        }
        if (_pdfSearchWindow is { IsVisible: true })
        {
            _pdfSearchWindow.Activate();
            _pdfQueryBox?.Focus();
            return;
        }

        var w = new Window { Title = "Search PDF", Owner = this, Width = 440, Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var panel = new StackPanel { Margin = new Thickness(12) };
        var input = new TextBox { Margin = new Thickness(0, 0, 0, 8) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        var search = new Button { Content = "Search", Width = 82 };
        var previous = new Button { Content = "Previous", Width = 82, Margin = new Thickness(6, 0, 0, 0) };
        var next = new Button { Content = "Next", Width = 82, Margin = new Thickness(6, 0, 0, 0) };
        var status = new TextBlock { Margin = new Thickness(0, 8, 0, 0) };
        search.Click += async (_, _) => await SearchPdfAsync(input.Text);
        previous.Click += async (_, _) => await StepPdfMatchAsync(-1);
        next.Click += async (_, _) => await StepPdfMatchAsync(1);
        input.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Enter) { args.Handled = true; await SearchPdfAsync(input.Text); }
            if (args.Key == Key.Escape) { args.Handled = true; w.Close(); }
        };
        buttons.Children.Add(search); buttons.Children.Add(previous); buttons.Children.Add(next);
        panel.Children.Add(input); panel.Children.Add(buttons); panel.Children.Add(status);
        w.Content = panel;
        _pdfSearchWindow = w; _pdfQueryBox = input; _pdfSearchStatus = status;
        w.Closed += (_, _) =>
        {
            _pdfSearchCts?.Cancel();
            _pdfSearchWindow = null; _pdfQueryBox = null; _pdfSearchStatus = null;
        };
        w.Show();
        input.Focus();
    }

    private async Task SearchPdfAsync(string query)
    {
        if (_book is not PdfBookSource pdf || string.IsNullOrWhiteSpace(query)) return;
        _pdfSearchCts?.Cancel();
        _pdfSearchCts = new CancellationTokenSource();
        var token = _pdfSearchCts.Token;
        _pdfMatches.Clear(); _pdfMatchCursor = -1;
        try
        {
            for (var page = 0; page < pdf.Descriptor.Pages.Count; page++)
            {
                token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(_book, pdf)) return;
                var response = await pdf.GetTextAsync(page, false, token).ConfigureAwait(true);
                var text = response.Text ?? "";
                for (var start = 0; start < text.Length;)
                {
                    var found = text.IndexOf(query, start, StringComparison.OrdinalIgnoreCase);
                    if (found < 0) break;
                    _pdfMatches.Add((page, found, query.Length));
                    start = found + Math.Max(1, query.Length);
                }
                if (_pdfSearchStatus is not null)
                    _pdfSearchStatus.Text = "Scanning " + (page + 1) + "/" + pdf.Descriptor.Pages.Count +
                        " pages; found " + _pdfMatches.Count + " occurrences";
            }
            if (_pdfSearchStatus is not null)
                _pdfSearchStatus.Text = _pdfMatches.Count + " results";
            if (_pdfMatches.Count > 0) await StepPdfMatchAsync(1);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (_pdfSearchStatus is not null) _pdfSearchStatus.Text = "Search failed: " + ex.Message;
        }
    }

    private async Task StepPdfMatchAsync(int direction)
    {
        if (_pdfMatches.Count == 0 || _book is not PdfBookSource) return;
        _pdfMatchCursor = (_pdfMatchCursor + direction + _pdfMatches.Count) % _pdfMatches.Count;
        var m = _pdfMatches[_pdfMatchCursor];
        await GoToPageAsync(m.Page);
        UpdatePdfHighlightRects();
        if (_pdfSearchStatus is not null)
            _pdfSearchStatus.Text = "Result " + (_pdfMatchCursor + 1) + "/" + _pdfMatches.Count +
                " — page " + (m.Page + 1);
        // The text geometry overlay is attached separately to the viewport pipeline.
    }

    private void PdfGoTo_Click(object sender, RoutedEventArgs e) => ShowPageJump();
    private void ShowPageJump()
    {
        if (_book is null) return;
        var window = new Window { Title = "Go to page", Owner = this, Width = 310, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var panel = new StackPanel { Margin = new Thickness(12) };
        var label = new ComboBox { ItemsSource = new[] { "Page number (1-based)", "PDF page label" }, SelectedIndex = 0 };
        var box = new TextBox { Text = (_pageIndex + 1).ToString(), Margin = new Thickness(0, 8, 0, 8) };
        var go = new Button { Content = "Go", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        go.Click += (_, _) => window.DialogResult = true;
        panel.Children.Add(label); panel.Children.Add(box); panel.Children.Add(go);
        window.Content = panel;
        if (window.ShowDialog() != true) return;
        var pages = _book.Descriptor.Pages;
        int index;
        if (label.SelectedIndex == 0 && int.TryParse(box.Text, out var pageNo)) index = pageNo - 1;
        else if (label.SelectedIndex == 1 && _book is PdfBookSource)
            index = pages.ToList().FindIndex(p => string.Equals(p.Name, box.Text, StringComparison.Ordinal));
        else index = -1;
        if (index < 0 || index >= pages.Count)
            MessageBox.Show(this, "Page not found.", "Comet", MessageBoxButton.OK, MessageBoxImage.Warning);
        else _ = GoToPageAsync(index);
    }

    private async void PdfProperties_Click(object sender, RoutedEventArgs e)
    {
        if (_book is not PdfBookSource pdf) return;
        try
        {
            var info = await pdf.GetMetadataAsync();
            if (!ReferenceEquals(_book, pdf)) return;
            var text = info.Metadata is null ? "" :
                string.Join(Environment.NewLine, info.Metadata.Select(p => p.Key + ": " + (string.IsNullOrEmpty(p.Value) ? "—" : p.Value)));
            MessageBox.Show(this, "Pages: " + pdf.Descriptor.Pages.Count + Environment.NewLine +
                text, "PDF Properties", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Comet"); }
    }

    private async Task<bool> FollowPdfLinkAsync(Point location)
    {
        if (_book is not PdfBookSource pdf) return false;
        for (var offset = 0; offset < _displayedPageCount; offset++)
        {
            var rect = Viewport.GetPageRect(offset);
            var index = _pageIndex + offset;
            if (rect.IsEmpty || !rect.Contains(location) || index >= pdf.Pages.Count) continue;
            var page = pdf.Pages[index];
            if (page.Width <= 0 || page.Height <= 0) return false;
            var unCropped = pdf.GetCrop(index).ToOriginal((location.X - rect.Left) / rect.Width,
                (location.Y - rect.Top) / rect.Height);
            var normal = PdfCoordinateMapper.DisplayToPdf(unCropped.U,
                unCropped.V, pdf.GetPageRotation(index));
            var x = normal.X * page.Width;
            var y = normal.Y * page.Height;
            try
            {
                var link = await pdf.GetLinkAsync(index, x, y).ConfigureAwait(true);
                if (!ReferenceEquals(_book, pdf)) return true;
                if (link.LinkPageIndex >= 0 && link.LinkPageIndex < pdf.Pages.Count)
                {
                    await GoToPageAsync(link.LinkPageIndex);
                    return true;
                }
                if (link.LinkUrl is { Length: > 0 } url &&
                    Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                    (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
                {
                    if (MessageBox.Show(this, "Open this external link in your browser?\n" + url,
                        "Comet PDF link", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to open PDF link: " + ex.Message, "Comet");
                return true;
            }
            return false;
        }
        return false;
    }

    private void ClosePdfUi()
    {
        _pdfSearchCts?.Cancel();
        _pdfMatches.Clear();
        _pdfSearchWindow?.Close();
        PdfOutlineTree.Items.Clear();
        PdfOutlineTabButton.Visibility = Visibility.Collapsed;
        ShowThumbnailTab();
    }
}
