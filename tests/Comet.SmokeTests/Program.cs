using System.IO.Compression;
using Comet.Core.Models;
using Comet.Core.Services;
using Comet.Infrastructure.Sources;

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

var names = new[] { "10.jpg", "2.jpg", "001.jpg", "1.jpg" };
Array.Sort(names, NaturalStringComparer.Instance);
Equal("1.jpg,001.jpg,2.jpg,10.jpg", string.Join(',', names), "natural sort");

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

    await using var book = await ZipBookSource.OpenAsync(zipPath);
    Equal(3, book.Descriptor.Pages.Count, "zip image filtering");
    Equal("page1.jpg", book.Descriptor.Pages[0].Name, "zip natural first");
    Equal("page2.jpg", book.Descriptor.Pages[1].Name, "zip natural second");
    Equal("page10.jpg", book.Descriptor.Pages[2].Name, "zip natural third");
    var bytes = await book.ReadPageBytesAsync(1);
    Check(bytes.SequenceEqual(new byte[] { 1, 2, 3, 4 }), "zip page bytes");
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
