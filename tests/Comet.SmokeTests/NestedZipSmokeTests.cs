using System.IO.Compression;
using System.Runtime.CompilerServices;
using Comet.Infrastructure.Sources;

internal static class NestedZipSmokeTests
{
    [ModuleInitializer]
    public static void Initialize()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"comet-nested-zip-smoke-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            var failures = RunAsync(temp).GetAwaiter().GetResult();
            if (failures.Count != 0)
                throw new InvalidOperationException("Nested ZIP smoke tests failed: " + string.Join("; ", failures));
        }
        finally
        {
            if (Directory.Exists(temp))
                Directory.Delete(temp, recursive: true);
        }
    }

    private static async Task<IReadOnlyList<string>> RunAsync(string tempRoot)
    {
        var failures = new List<string>();
        var outerPath = Path.Combine(tempRoot, "nested-outer.zip");

        var bZip = CreateZipBytes(
            ("page10.webp", new byte[] { 10 }),
            ("page2.webp", new byte[] { 2 }),
            ("page1.webp", new byte[] { 1 }),
            ("notes.txt", new byte[] { 99 }));

        var dZip = CreateZipBytes(
            ("page1.webp", new byte[] { 31 }));

        var cCbz = CreateZipBytes(
            ("page1.webp", new byte[] { 21 }),
            ("z-D.zip", dZip));

        using (var fs = File.Create(outerPath))
        using (var outer = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            WriteEntry(outer, "00-cover.webp", new byte[] { 0 });
            WriteEntry(outer, "B.zip", bZip);
            WriteEntry(outer, "C.cbz", cCbz);
            WriteEntry(outer, "ignored.txt", new byte[] { 88 });
        }

        await using (var book = await ZipBookSource.OpenAsync(outerPath))
        {
            Equal(6, book.Descriptor.Pages.Count, "nested zip flattened page count", failures);
            Equal(Path.GetFullPath(outerPath), Path.GetFullPath(book.Descriptor.Path), "nested zip keeps outer source path", failures);

            var expectedNames = new[]
            {
                "00-cover.webp",
                "B.zip/page1.webp",
                "B.zip/page2.webp",
                "B.zip/page10.webp",
                "C.cbz/page1.webp",
                "C.cbz/z-D.zip/page1.webp"
            };

            for (var i = 0; i < expectedNames.Length && i < book.Descriptor.Pages.Count; i++)
                Equal(expectedNames[i], book.Descriptor.Pages[i].Name, $"nested zip page name {i}", failures);

            var expectedBytes = new byte[] { 0, 1, 2, 10, 21, 31 };
            for (var i = 0; i < expectedBytes.Length && i < book.Descriptor.Pages.Count; i++)
            {
                var bytes = await book.ReadPageBytesAsync(i);
                if (bytes.Length != 1 || bytes[0] != expectedBytes[i])
                    failures.Add($"nested zip page bytes {i}: expected={expectedBytes[i]}, actual={string.Join(',', bytes)}");
            }
        }

        var factory = new BookSourceFactory();
        await using (var factoryBook = await factory.OpenAsync(outerPath))
        {
            if (factoryBook is not ZipBookSource)
                failures.Add("factory keeps nested zip on system zip source");
            Equal(6, factoryBook.Descriptor.Pages.Count, "factory nested zip page count", failures);
        }

        return failures;
    }

    private static byte[] CreateZipBytes(params (string Name, byte[] Bytes)[] entries)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, bytes) in entries)
                WriteEntry(archive, name, bytes);
        }
        return memory.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void Equal<T>(T expected, T actual, string name, List<string> failures) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            failures.Add($"{name}: expected={expected}, actual={actual}");
    }
}
