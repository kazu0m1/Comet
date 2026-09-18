using System.IO.Compression;
using Comet.Core.Models;
using Comet.Core.Services;
using Comet.Infrastructure.Navigation;
using Comet.Infrastructure.Persistence;
using Comet.Infrastructure.Sources;
using Comet.Platform.Windows.Imaging;

var failures = new List<string>();

void Check(bool condition, string name)
{
    if (!condition) failures.Add(name);
}

void Equal<T>(T expected, T actual, string name) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        failures.Add($"{name}: expected={expected}, actual={actual}");
}

void Near(double expected, double actual, string name, double tolerance = 0.000001)
{
    if (Math.Abs(expected - actual) > tolerance)
        failures.Add($"{name}: expected={expected}, actual={actual}");
}

var names = new[] { "10.jpg", "2.jpg", "001.jpg", "1.jpg" };
Array.Sort(names, NaturalStringComparer.Instance);
Equal("1.jpg,001.jpg,2.jpg,10.jpg", string.Join(',', names), "natural sort");

var unicodeNames = new[] { "ページ10.jpg", "ページ2.jpg", "ページ1.jpg" };
Array.Sort(unicodeNames, NaturalStringComparer.Instance);
Equal("ページ1.jpg,ページ2.jpg,ページ10.jpg", string.Join(',', unicodeNames), "unicode natural sort");

var best = FitCalculator.CalculateScale(new PixelSize(1000, 2000), new ViewportSize(1000, 1000), FitMode.BestFit, true);
Equal(0.5, best, "best fit");
var stretch = FitCalculator.CalculateScale(new PixelSize(100, 100), new ViewportSize(1000, 500), FitMode.BestFit, true);
Equal(5.0, stretch, "stretch small images");
var noStretch = FitCalculator.CalculateScale(new PixelSize(100, 100), new ViewportSize(1000, 500), FitMode.BestFit, false);
Equal(1.0, noStretch, "do not stretch small images");

Equal(0, SpreadPlanner.NormalizeStartIndex(0, 10, PageLayoutMode.DoublePage), "cover alone");
Equal(1, SpreadPlanner.NormalizeStartIndex(2, 10, PageLayoutMode.DoublePage), "double spread normalize");
Equal(3, SpreadPlanner.NextStartIndex(1, 2, 10), "next spread");
Equal(1, SpreadPlanner.PreviousStartIndex(3, PageLayoutMode.DoublePage), "previous spread");

var zoom = new ZoomSessionState(FitMode.BestFit);
zoom.AdjustTemporaryZoom(0.8);
zoom.OnAdjacentArchiveOpened();
Near(0.8, zoom.TemporaryZoomFactor, "temporary zoom survives adjacent archive");
zoom.ResetForNewProcess();
Near(1.0, zoom.TemporaryZoomFactor, "temporary zoom resets for new process");

Check(SupportedImages.IsSupported("PAGE.JPG"), "supported image extension is case-insensitive");
Check(SupportedImages.IsSupported("page.tiff"), "tiff supported");
Check(SupportedImages.IsSupported("page.webp"), "webp supported extension");
Check(!SupportedImages.IsSupported("notes.txt"), "non-image rejected");

var webpFixture = Convert.FromBase64String("UklGRhwAAABXRUJQVlA4TA8AAAAvAYAAAAcQ/Y/+ByKi/wEA");
var windowsDecoder = new WpfBitmapDecoder();
var webpSize = windowsDecoder.Probe(webpFixture);
Equal(2, webpSize.Width, "webp probe width");
Equal(3, webpSize.Height, "webp probe height");
var decodedWebp = windowsDecoder.Decode(webpFixture, 1);
Equal(1, decodedWebp.PixelWidth, "webp decode-to-size width");
Equal(2, decodedWebp.PixelHeight, "webp decode-to-size height");

var temp = Path.Combine(Path.GetTempPath(), $"comet-smoke-{Guid.NewGuid():N}");
Directory.CreateDirectory(temp);
try
{
    var zipPath = Path.Combine(temp, "book.zip");
    using (var fs = File.Create(zipPath))
    using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
    {
        foreach (var name in new[] { "page10.jpg", "page2.jpg", "page1.jpg", "notes.txt" })
        {
            var entry = zip.CreateEntry(name);
            using var stream = entry.Open();
            stream.Write(new byte[] { 1, 2, 3, 4 });
        }
    }

    await using (var book = await ZipBookSource.OpenAsync(zipPath))
    {
        Equal(3, book.Descriptor.Pages.Count, "zip image filtering");
        Equal("page1.jpg", book.Descriptor.Pages[0].Name, "zip natural first");
        Equal("page2.jpg", book.Descriptor.Pages[1].Name, "zip natural second");
        Equal("page10.jpg", book.Descriptor.Pages[2].Name, "zip natural third");
        var bytes = await book.ReadPageBytesAsync(1);
        Check(bytes.SequenceEqual(new byte[] { 1, 2, 3, 4 }), "zip page bytes");
    }

    var adjacentRoot = Path.Combine(temp, "adjacent");
    Directory.CreateDirectory(adjacentRoot);
    var archive1 = Path.Combine(adjacentRoot, "第1巻.zip");
    var archive2 = Path.Combine(adjacentRoot, "第2巻.cbz");
    var archive10 = Path.Combine(adjacentRoot, "第10巻.zip");
    File.WriteAllBytes(archive1, Array.Empty<byte>());
    File.WriteAllBytes(archive2, Array.Empty<byte>());
    File.WriteAllBytes(archive10, Array.Empty<byte>());
    var finder = new AdjacentArchiveFinder();
    Equal(Path.GetFullPath(archive10), Path.GetFullPath(finder.FindNext(archive2)!), "adjacent archive natural next");
    Equal(Path.GetFullPath(archive1), Path.GetFullPath(finder.FindPrevious(archive2)!), "adjacent archive natural previous");

    var stateRoot = Path.Combine(temp, "state");
    var stateStore = new JsonBookStateStore(stateRoot);
    var sourcePath = Path.Combine(temp, "fictional-book.zip");
    var saved = new BookState(
        42,
        new[] { new Bookmark(42, DateTimeOffset.Parse("2026-09-18T00:00:00+00:00"), "checkpoint") },
        123456,
        DateTimeOffset.Parse("2026-09-18T01:00:00+00:00"));
    await stateStore.SaveAsync(sourcePath, saved);
    var loaded = await stateStore.LoadAsync(sourcePath);
    Check(loaded is not null, "book state roundtrip returns state");
    if (loaded is not null)
    {
        Equal(42, loaded.LastPageIndex, "book state last page");
        Equal(1, loaded.Bookmarks.Count, "book state bookmark count");
        Equal(42, loaded.Bookmarks[0].PageIndex, "book state bookmark page");
        Equal("checkpoint", loaded.Bookmarks[0].Label!, "book state bookmark label");
        Equal(123456L, loaded.SourceLength, "book state source length");
    }

    var settingsRoot = Path.Combine(temp, "settings");
    Directory.CreateDirectory(settingsRoot);
    var settingsStore = new JsonSettingsStore(settingsRoot);
    var explicitSettings = new AppSettings
    {
        ShowThumbnails = false,
        ThumbnailWidth = 88,
        FitMode = FitMode.FitWidth
    };
    await settingsStore.SaveAsync(explicitSettings);
    var settingsRoundtrip = await settingsStore.LoadAsync();
    Equal(false, settingsRoundtrip.ShowThumbnails, "settings preserve thumbnail visibility");
    Equal(88d, settingsRoundtrip.ThumbnailWidth, "settings preserve thumbnail width");
    Equal(FitMode.FitWidth, settingsRoundtrip.FitMode, "settings preserve fit mode");

    await File.WriteAllTextAsync(
        Path.Combine(settingsRoot, "settings.json"),
        """
        {
          "SettingsSchemaVersion": 1,
          "ShowThumbnails": false,
          "ThumbnailWidth": 180
        }
        """);
    var migrated = await settingsStore.LoadAsync();
    Equal(2, migrated.SettingsSchemaVersion, "settings migrate schema");
    Equal(false, migrated.ShowThumbnails, "settings migration preserves explicit visibility");
    Equal(72d, migrated.ThumbnailWidth, "settings migration adopts compact thumbnails");

    var corruptRoot = Path.Combine(temp, "corrupt-settings");
    Directory.CreateDirectory(corruptRoot);
    await File.WriteAllTextAsync(Path.Combine(corruptRoot, "settings.json"), "{ definitely not json");
    var corruptSettings = await new JsonSettingsStore(corruptRoot).LoadAsync();
    Equal(2, corruptSettings.SettingsSchemaVersion, "corrupt settings fallback schema");
    Equal(FitMode.BestFit, corruptSettings.FitMode, "corrupt settings fallback fit");
    Equal(true, corruptSettings.ShowThumbnails, "corrupt settings fallback thumbnails");
}
finally
{
    Directory.Delete(temp, recursive: true);
}

if (failures.Count == 0)
{
    Console.WriteLine("Comet smoke tests: PASS");
    return 0;
}

Console.Error.WriteLine("Comet smoke tests: FAIL");
foreach (var failure in failures) Console.Error.WriteLine(" - " + failure);
return 1;
