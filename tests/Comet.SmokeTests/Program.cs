using System.IO;
using System.IO.Compression;
using Comet.Core.Models;
using Comet.Core.Services;
using Comet.Infrastructure.Navigation;
using Comet.Infrastructure.Persistence;
using Comet.Infrastructure.Sources;
using Comet.Platform.Windows.Imaging;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;

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

Near(1.0, DpiScaleCalculator.ManualDipScale(1.0), "manual 100 at 100 percent DPI");
Near(0.8, DpiScaleCalculator.ManualDipScale(1.25), "manual 100 at 125 percent DPI");
Near(2.0 / 3.0, DpiScaleCalculator.ManualDipScale(1.5), "manual 100 at 150 percent DPI");
Near(0.5, DpiScaleCalculator.ManualDipScale(2.0), "manual 100 at 200 percent DPI");
Near(1.0, DpiScaleCalculator.PhysicalZoom(800, 1200, 1.5), "physical zoom reports true 100 percent");
Near(0.75, DpiScaleCalculator.PhysicalZoom(600, 1200, 1.5), "physical zoom reports true 75 percent");

var weightedCache = new LruCache<int, string>(capacity: 3, maxWeight: 5, weightSelector: value => value.Length);
weightedCache.Set(1, "aaa");
weightedCache.Set(2, "bbb");
Check(!weightedCache.TryGet(1, out _), "weighted cache evicts least-recent item when over budget");
Check(weightedCache.TryGet(2, out var keptWeighted) && keptWeighted == "bbb", "weighted cache keeps newest item");
weightedCache.Set(3, "1234567");
Check(!weightedCache.TryGet(2, out _), "oversized newest item evicts older entries");
Check(weightedCache.TryGet(3, out var oversizedWeighted) && oversizedWeighted == "1234567", "weighted cache keeps one oversized newest item");
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

foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp" })
    Check(SupportedImages.IsSupported("PAGE" + extension.ToUpperInvariant()), $"supported image extension {extension}");

foreach (var extension in new[] { ".zip", ".cbz", ".rar", ".cbr", ".7z", ".cb7" })
    Check(SupportedArchives.IsSupported("comic" + extension.ToUpperInvariant()), $"supported archive extension {extension}");

Check(SupportedArchives.UsesSystemZip("comic.zip"), "zip keeps system zip path");
Check(SupportedArchives.UsesSystemZip("comic.cbz"), "cbz keeps system zip path");
foreach (var extension in new[] { ".rar", ".cbr", ".7z", ".cb7" })
    Check(SupportedArchives.UsesSharpCompress("comic" + extension), $"SharpCompress archive path {extension}");

Check(!SupportedImages.IsSupported("notes.txt"), "non-image rejected");
Check(!SupportedArchives.IsSupported("comic.tar"), "unsupported archive rejected");

var jpegHeaderFixture = new byte[]
{
    0xFF, 0xD8,
    0xFF, 0xC0, 0x00, 0x11,
    0x08, 0x08, 0x00, 0x06, 0x53, 0x03,
    0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00,
    0xFF, 0xD9
};
var windowsDecoder = new WpfBitmapDecoder();
var jpegHeaderSize = windowsDecoder.Probe(jpegHeaderFixture);
Equal(1619, jpegHeaderSize.Width, "jpeg header probe width without bitmap decode");
Equal(2048, jpegHeaderSize.Height, "jpeg header probe height without bitmap decode");

var webpFixture = Convert.FromBase64String("UklGRhwAAABXRUJQVlA4TA8AAAAvAYAAAAcQ/Y/+ByKi/wEA");
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
    var imageFolder = Path.Combine(temp, "画像フォルダー");
    Directory.CreateDirectory(imageFolder);
    await File.WriteAllBytesAsync(Path.Combine(imageFolder, "ページ10.webp"), webpFixture);
    await File.WriteAllBytesAsync(Path.Combine(imageFolder, "ページ2.webp"), webpFixture);
    await File.WriteAllBytesAsync(Path.Combine(imageFolder, "ページ1.webp"), webpFixture);
    await File.WriteAllTextAsync(Path.Combine(imageFolder, "notes.txt"), "ignored");

    await using (var folderBook = await FolderBookSource.OpenAsync(imageFolder))
    {
        Equal(3, folderBook.Descriptor.Pages.Count, "folder image filtering");
        Equal("ページ1.webp", folderBook.Descriptor.Pages[0].Name, "folder unicode natural first");
        Equal("ページ2.webp", folderBook.Descriptor.Pages[1].Name, "folder unicode natural second");
        Equal("ページ10.webp", folderBook.Descriptor.Pages[2].Name, "folder unicode natural third");
        var folderBytes = await folderBook.ReadPageBytesAsync(1);
        Check(folderBytes.SequenceEqual(webpFixture), "folder page bytes");
    }

    var sourceFactory = new BookSourceFactory();
    await using (var fromFolder = await sourceFactory.OpenAsync(imageFolder))
        Check(fromFolder is FolderBookSource, "factory routes folder to folder source");

    await using (var fromImage = await sourceFactory.OpenAsync(Path.Combine(imageFolder, "ページ2.webp")))
    {
        Check(fromImage is FolderBookSource, "factory routes individual image through containing folder");
        Equal(3, fromImage.Descriptor.Pages.Count, "individual image factory exposes folder pages");
    }

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

    await using (var factoryZip = await sourceFactory.OpenAsync(zipPath))
        Check(factoryZip is ZipBookSource, "factory routes zip to system zip source");

    await using (var book = await ZipBookSource.OpenAsync(zipPath))
    {
        Equal(3, book.Descriptor.Pages.Count, "zip image filtering");
        Equal("page1.jpg", book.Descriptor.Pages[0].Name, "zip natural first");
        Equal("page2.jpg", book.Descriptor.Pages[1].Name, "zip natural second");
        Equal("page10.jpg", book.Descriptor.Pages[2].Name, "zip natural third");
        var bytes = await book.ReadPageBytesAsync(1);
        Check(bytes.SequenceEqual(new byte[] { 1, 2, 3, 4 }), "zip page bytes");
    }

    var damagedZipPath = Path.Combine(temp, "damaged-page.zip");
    using (var fs = File.Create(damagedZipPath))
    using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
    {
        var first = zip.CreateEntry("page1.webp");
        await using (var stream = first.Open())
            await stream.WriteAsync(webpFixture);

        var broken = zip.CreateEntry("page2.jpg");
        await using (var stream = broken.Open())
            await stream.WriteAsync(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44 });

        var third = zip.CreateEntry("page3.webp");
        await using (var stream = third.Open())
            await stream.WriteAsync(webpFixture);
    }

    await using (var damagedBook = await ZipBookSource.OpenAsync(damagedZipPath))
    {
        Equal(3, damagedBook.Descriptor.Pages.Count, "damaged-page zip keeps all image entries");

        var firstImage = windowsDecoder.Decode(await damagedBook.ReadPageBytesAsync(0));
        Equal(2, firstImage.PixelWidth, "valid page before damaged page decodes");

        var damagedFailed = false;
        try
        {
            windowsDecoder.Decode(await damagedBook.ReadPageBytesAsync(1));
        }
        catch
        {
            damagedFailed = true;
        }
        Check(damagedFailed, "damaged page reports decode failure");

        var pageAfterDamage = windowsDecoder.Decode(await damagedBook.ReadPageBytesAsync(2));
        Equal(2, pageAfterDamage.PixelWidth, "page after damaged page still decodes");
    }

    var cb7Path = Path.Combine(temp, "comic.cb7");
    await using (var stream = File.Create(cb7Path))
    {
        using var writer = WriterFactory.OpenWriter(
            stream,
            ArchiveType.SevenZip,
            new SevenZipWriterOptions(CompressionType.LZMA2)
            {
                CompressHeader = true
            });

        using var page10 = new MemoryStream(webpFixture);
        writer.Write("page10.webp", page10, DateTime.UtcNow);
        using var page2 = new MemoryStream(webpFixture);
        writer.Write("page2.webp", page2, DateTime.UtcNow);
        using var page1 = new MemoryStream(webpFixture);
        writer.Write("page1.webp", page1, DateTime.UtcNow);
    }

    await using (var cb7 = await ArchiveBookSource.OpenAsync(cb7Path))
    {
        Equal(3, cb7.Descriptor.Pages.Count, "cb7 image filtering");
        Equal("page1.webp", cb7.Descriptor.Pages[0].Name, "cb7 natural first");
        Equal("page2.webp", cb7.Descriptor.Pages[1].Name, "cb7 natural second");
        Equal("page10.webp", cb7.Descriptor.Pages[2].Name, "cb7 natural third");
        var bytes = await cb7.ReadPageBytesAsync(1);
        Check(bytes.SequenceEqual(webpFixture), "cb7 page bytes");
        var decoded = windowsDecoder.Decode(bytes);
        Equal(2, decoded.PixelWidth, "cb7 WebP decode");
    }

    var cbrFixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SharpCompress_Rar.cbr");
    Check(File.Exists(cbrFixturePath), "cbr fixture copied to test output");
    var cbrFactory = new BookSourceFactory();
    await using (var cbr = await cbrFactory.OpenAsync(cbrFixturePath))
    {
        Check(cbr.Descriptor.Pages.Count > 0, "cbr real RAR fixture exposes image pages");
        var cbrBytes = await cbr.ReadPageBytesAsync(0);
        Check(cbrBytes.Length > 0, "cbr real RAR fixture page bytes");
        var cbrImage = windowsDecoder.Decode(cbrBytes);
        Check(cbrImage.PixelWidth > 0 && cbrImage.PixelHeight > 0, "cbr real RAR fixture JPEG decodes");
    }

    foreach (var (fixtureName, label) in new[]
    {
        ("SharpCompress_Rar_Solid.cbr", "solid cbr"),
        ("SharpCompress_7Zip_Solid.cb7", "solid cb7")
    })
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        Check(File.Exists(fixturePath), $"{label} fixture copied to test output");
        var fixtureFactory = new BookSourceFactory();
        await using var fixtureBook = await fixtureFactory.OpenAsync(fixturePath);
        Check(fixtureBook.Descriptor.Pages.Count > 0, $"{label} exposes image pages");
        var fixtureBytes = await fixtureBook.ReadPageBytesAsync(fixtureBook.Descriptor.Pages.Count - 1);
        Check(fixtureBytes.Length > 0, $"{label} last image bytes");
        var fixtureImage = windowsDecoder.Decode(fixtureBytes);
        Check(fixtureImage.PixelWidth > 0 && fixtureImage.PixelHeight > 0, $"{label} image decodes");
    }

    var adjacentRoot = Path.Combine(temp, "adjacent");
    Directory.CreateDirectory(adjacentRoot);
    var archive1 = Path.Combine(adjacentRoot, "第1巻.zip");
    var archive2 = Path.Combine(adjacentRoot, "第2巻.cbz");
    var archive3 = Path.Combine(adjacentRoot, "第3巻.rar");
    var archive4 = Path.Combine(adjacentRoot, "第4巻.cbr");
    var archive5 = Path.Combine(adjacentRoot, "第5巻.7z");
    var archive6 = Path.Combine(adjacentRoot, "第6巻.cb7");
    var archive10 = Path.Combine(adjacentRoot, "第10巻.zip");
    foreach (var archive in new[] { archive1, archive2, archive3, archive4, archive5, archive6, archive10 })
        File.WriteAllBytes(archive, Array.Empty<byte>());

    var finder = new AdjacentArchiveFinder();
    Equal(Path.GetFullPath(archive3), Path.GetFullPath(finder.FindNext(archive2)!), "adjacent archive includes rar");
    Equal(Path.GetFullPath(archive4), Path.GetFullPath(finder.FindNext(archive3)!), "adjacent archive includes cbr");
    Equal(Path.GetFullPath(archive5), Path.GetFullPath(finder.FindNext(archive4)!), "adjacent archive includes 7z");
    Equal(Path.GetFullPath(archive6), Path.GetFullPath(finder.FindNext(archive5)!), "adjacent archive includes cb7");
    Equal(Path.GetFullPath(archive10), Path.GetFullPath(finder.FindNext(archive6)!), "adjacent archive natural next");
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

    var trackedBookPath = Path.Combine(temp, "tracked-book.zip");
    await File.WriteAllBytesAsync(trackedBookPath, new byte[] { 1, 2, 3, 4, 5 });
    var trackedInfo = new FileInfo(trackedBookPath);
    var matchingState = new BookState(
        7,
        Array.Empty<Bookmark>(),
        trackedInfo.Length,
        trackedInfo.LastWriteTimeUtc);
    Check(BookStateSourceValidator.Matches(matchingState, trackedBookPath), "book state matches unchanged source");

    await File.AppendAllTextAsync(trackedBookPath, "changed");
    Check(!BookStateSourceValidator.Matches(matchingState, trackedBookPath), "book state rejected after source changes");

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

    var normalizedSettings = AppSettingsNormalizer.Normalize(new AppSettings
    {
        FitMode = (FitMode)999,
        ReadingDirection = (ReadingDirection)999,
        PageLayoutMode = (PageLayoutMode)999,
        ThumbnailWidth = 9999,
        SmartScrollFraction = -3,
        ArrowScrollPixels = double.PositiveInfinity
    });
    Equal(FitMode.BestFit, normalizedSettings.FitMode, "invalid fit mode normalized");
    Equal(ReadingDirection.RightToLeft, normalizedSettings.ReadingDirection, "invalid reading direction normalized");
    Equal(PageLayoutMode.DoublePage, normalizedSettings.PageLayoutMode, "invalid layout mode normalized");
    Equal(200d, normalizedSettings.ThumbnailWidth, "thumbnail width clamped");
    Equal(0.10d, normalizedSettings.SmartScrollFraction, "smart scroll fraction clamped");
    Equal(70d, normalizedSettings.ArrowScrollPixels, "non-finite arrow scroll falls back");

    await File.WriteAllTextAsync(
        Path.Combine(settingsRoot, "settings.json"),
        """
        {
          "SettingsSchemaVersion": 2,
          "FitMode": 999,
          "ReadingDirection": 999,
          "PageLayoutMode": 999,
          "ThumbnailWidth": -500,
          "SmartScrollFraction": 99,
          "ArrowScrollPixels": -10
        }
        """);
    var sanitizedFromDisk = await settingsStore.LoadAsync();
    Equal(FitMode.BestFit, sanitizedFromDisk.FitMode, "disk invalid fit mode normalized");
    Equal(ReadingDirection.RightToLeft, sanitizedFromDisk.ReadingDirection, "disk invalid direction normalized");
    Equal(PageLayoutMode.DoublePage, sanitizedFromDisk.PageLayoutMode, "disk invalid layout normalized");
    Equal(48d, sanitizedFromDisk.ThumbnailWidth, "disk thumbnail width clamped");
    Equal(1.0d, sanitizedFromDisk.SmartScrollFraction, "disk smart scroll clamped");
    Equal(10d, sanitizedFromDisk.ArrowScrollPixels, "disk arrow scroll clamped");
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
