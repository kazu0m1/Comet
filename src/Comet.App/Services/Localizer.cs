using System.Globalization;

namespace Comet.App.Services;

public sealed class Localizer
{
    private readonly bool _ja = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase);

    public string this[string key] => _ja ? Ja.GetValueOrDefault(key, En.GetValueOrDefault(key, key)) : En.GetValueOrDefault(key, key);

    private static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>
    {
        ["File"] = "_File", ["Open"] = "_Open...", ["OpenFolder"] = "Open _Folder...", ["Close"] = "_Close", ["Exit"] = "E_xit",
        ["View"] = "_View", ["BestFit"] = "Best Fit", ["FitWidth"] = "Fit Width", ["FitHeight"] = "Fit Height",
        ["Manual"] = "Manual 100%", ["DoublePage"] = "Double Page", ["MangaMode"] = "Manga Mode", ["Stretch"] = "Stretch Small Images",
        ["Fullscreen"] = "Fullscreen", ["Thumbnails"] = "Thumbnails", ["Bookmarks"] = "_Bookmarks", ["AddBookmark"] = "Add Bookmark", ["EditBookmarks"] = "Edit Bookmarks...",
        ["Navigate"] = "_Navigate", ["PreviousPage"] = "Previous Page", ["NextPage"] = "Next Page", ["PreviousArchive"] = "Previous Archive",
        ["NextArchive"] = "Next Archive", ["Ready"] = "Ready", ["Opening"] = "Opening...", ["Closing"] = "Closing...", ["NoImages"] = "No supported images found.",
        ["OpenComic"] = "Open comic archive", ["Error"] = "Error", ["BookmarksTitle"] = "Bookmarks", ["NoBookmarks"] = "No bookmarks yet.",
        ["Page"] = "Page", ["BookmarkAdded"] = "Bookmark added", ["ImageSize"] = "Image", ["Zoom"] = "Zoom", ["Previous"] = "Previous", ["Next"] = "Next", ["ToolbarBest"] = "Best", ["ToolbarWidth"] = "Width", ["ToolbarHeight"] = "Height", ["ToolbarManga"] = "Manga"
    };

    private static readonly IReadOnlyDictionary<string, string> Ja = new Dictionary<string, string>
    {
        ["File"] = "ファイル(_F)", ["Open"] = "開く(_O)...", ["OpenFolder"] = "フォルダーを開く...", ["Close"] = "閉じる", ["Exit"] = "終了(_X)",
        ["View"] = "表示(_V)", ["BestFit"] = "ベストフィット", ["FitWidth"] = "幅に合わせる", ["FitHeight"] = "高さに合わせる",
        ["Manual"] = "原寸 100%", ["DoublePage"] = "見開き", ["MangaMode"] = "漫画モード（右綴じ）", ["Stretch"] = "小さい画像も拡大",
        ["Fullscreen"] = "全画面", ["Thumbnails"] = "サムネイル", ["Bookmarks"] = "ブックマーク(_B)", ["AddBookmark"] = "ブックマーク追加", ["EditBookmarks"] = "ブックマーク一覧...",
        ["Navigate"] = "移動(_N)", ["PreviousPage"] = "前のページ", ["NextPage"] = "次のページ", ["PreviousArchive"] = "前の書庫",
        ["NextArchive"] = "次の書庫", ["Ready"] = "準備完了", ["Opening"] = "読み込み中...", ["Closing"] = "終了処理中...", ["NoImages"] = "対応画像がありません。",
        ["OpenComic"] = "漫画書庫を開く", ["Error"] = "エラー", ["BookmarksTitle"] = "ブックマーク", ["NoBookmarks"] = "ブックマークはありません。",
        ["Page"] = "ページ", ["BookmarkAdded"] = "ブックマークを追加しました", ["ImageSize"] = "画像", ["Zoom"] = "倍率", ["Previous"] = "前へ", ["Next"] = "次へ", ["ToolbarBest"] = "全体", ["ToolbarWidth"] = "幅", ["ToolbarHeight"] = "高さ", ["ToolbarManga"] = "漫画"
    };
}
