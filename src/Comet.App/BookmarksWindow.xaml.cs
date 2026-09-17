using System.Windows;
using System.Windows.Input;
using Comet.App.Services;
using Comet.Core.Models;

namespace Comet.App;

public partial class BookmarksWindow : Window
{
    private readonly IReadOnlyList<Bookmark> _bookmarks;

    public BookmarksWindow(IReadOnlyList<Bookmark> bookmarks, Localizer localizer)
    {
        _bookmarks = bookmarks;
        InitializeComponent();
        Title = localizer["BookmarksTitle"];
        EmptyText.Text = bookmarks.Count == 0 ? localizer["NoBookmarks"] : string.Empty;
        BookmarkList.ItemsSource = bookmarks.Select(b => $"{localizer["Page"]} {b.PageIndex + 1} — {b.CreatedAt.LocalDateTime:g}").ToArray();
        OpenButton.IsEnabled = bookmarks.Count > 0;
        if (bookmarks.Count > 0) BookmarkList.SelectedIndex = 0;
    }

    public int? SelectedPageIndex { get; private set; }

    private void Open_Click(object sender, RoutedEventArgs e) => AcceptSelection();
    private void BookmarkList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => AcceptSelection();

    private void AcceptSelection()
    {
        var index = BookmarkList.SelectedIndex;
        if (index < 0 || index >= _bookmarks.Count) return;
        SelectedPageIndex = _bookmarks[index].PageIndex;
        DialogResult = true;
    }
}
