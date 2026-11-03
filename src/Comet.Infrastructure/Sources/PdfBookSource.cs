using Comet.Core.Abstractions;
using Comet.Core.Models;

namespace Comet.Infrastructure.Sources;

public sealed class PdfBookSource : IRasterBookSource
{
    private readonly PdfWorkerClient _worker;
    private readonly PdfPageInfo[] _pages;
    private PdfBookSource(string path, PdfWorkerClient worker, PdfReply info)
    {
        _worker = worker;
        _pages = info.Pages?.ToArray() ?? [];
        Permissions = info.Permissions;
        Descriptor = new BookDescriptor(
            path, path, Path.GetFileName(path),
            _pages.Select((page, index) =>
                new PageDescriptor(index,
                    string.IsNullOrWhiteSpace(page.Label) ? (index + 1).ToString() : page.Label,
                    ".pdf", 0)).ToArray());
    }

    public BookDescriptor Descriptor { get; }
    public ulong Permissions { get; }
    public IReadOnlyList<PdfPageInfo> Pages => _pages;
    public bool CopyAllowed => (Permissions & 0x10UL) != 0 || Permissions == ulong.MaxValue;

    public static async ValueTask<PdfBookSource> OpenAsync(string path, CancellationToken token = default, string? password = null)
    {
        var worker = PdfWorkerClient.Start();
        try
        {
            var info = await worker.SendAsync(new { Command = "open", Path = path, Password = password }, token).ConfigureAwait(false);
            return new PdfBookSource(path, worker, info);
        }
        catch
        {
            await worker.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public ValueTask<byte[]> ReadPageBytesAsync(int pageIndex, CancellationToken token = default)
        => RenderPageBytesAsync(pageIndex, 1600, token);

    public async ValueTask<byte[]> RenderPageBytesAsync(int pageIndex, int width, CancellationToken token = default)
    {
        if ((uint)pageIndex >= (uint)_pages.Length) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        var response = await _worker.SendAsync(new { Command = "render", Index = pageIndex, Width = width, Rotation = 0 }, token).ConfigureAwait(false);
        var filename = response.ImagePath ?? throw new IOException("PDF worker did not return a rendered page");
        try { return await File.ReadAllBytesAsync(filename, token).ConfigureAwait(false); }
        finally { try { File.Delete(filename); } catch (IOException) { } }
    }

    public Task<PdfReply> GetTextAsync(int pageIndex, bool includeBoxes, CancellationToken token = default)
        => _worker.SendAsync(new { Command = "text", Index = pageIndex, WithBoxes = includeBoxes }, token);

    public Task<PdfReply> GetOutlineAsync(CancellationToken token = default)
        => _worker.SendAsync(new { Command = "outline" }, token);

    public Task<PdfReply> GetMetadataAsync(CancellationToken token = default)
        => _worker.SendAsync(new { Command = "metadata" }, token);

    public ValueTask DisposeAsync() => _worker.DisposeAsync();
}
