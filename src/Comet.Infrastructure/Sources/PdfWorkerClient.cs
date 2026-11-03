using System.Diagnostics;
using System.Text.Json;

namespace Comet.Infrastructure.Sources;

// The untrusted PDF is only parsed by the out-of-process, V8-free PDFium worker.
// No PDF native library is loaded in Comet's WPF process.
public sealed class PdfWorkerClient : IAsyncDisposable
{
    private readonly Process _process;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private PdfWorkerClient(Process process) => _process = process;

    public static PdfWorkerClient Start()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "pdf-worker", "Comet.PdfWorker.exe");
        if (!File.Exists(exe))
        {
            var configuration = 
#if DEBUG
                "Debug";
#else
                "Release";
#endif
            var development = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "Comet.PdfWorker", "bin", configuration, "net10.0", "win-x64", "Comet.PdfWorker.exe"));
            if (File.Exists(development)) exe = development;
        }
        if (!File.Exists(exe))
            throw new FileNotFoundException("Comet PDF worker has not been published.", exe);

        var info = new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        return new PdfWorkerClient(Process.Start(info) ?? throw new IOException("Failed to start PDF worker"));
    }

    public async Task<PdfReply> SendAsync(object request, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_process.HasExited) throw new IOException("PDF worker exited unexpectedly");
            // Separate native crash from the reader. Cancelled/closed clients kill the worker.
            var line = JsonSerializer.Serialize(request, Json);
            cancellationToken.ThrowIfCancellationRequested();
            await _process.StandardInput.WriteLineAsync(line).ConfigureAwait(false);
            await _process.StandardInput.FlushAsync().ConfigureAwait(false);
            // After a request is sent, finish reading its matching response even if
            // the caller cancels. Otherwise a stale reply can be misrouted to the
            // next operation. Native hangs time out rather than blocking forever.
            var replyLine = await _process.StandardOutput.ReadLineAsync()
                .WaitAsync(TimeSpan.FromSeconds(90)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(replyLine)) throw new IOException("PDF worker disconnected");
            var reply = JsonSerializer.Deserialize<PdfReply>(replyLine, Json)
                ?? throw new IOException("Invalid PDF worker response");
            if (!reply.Ok)
                throw new PdfWorkerException(reply.Error ?? "PDF worker operation failed", reply.PdfError);
            return reply;
        }
        finally { _mutex.Release(); }
    }

    public ValueTask DisposeAsync()
    {
        try { if (!_process.HasExited) _process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        _process.Dispose();
        // A disposed semaphore must not race any in-flight operation.
        return ValueTask.CompletedTask;
    }
}

public sealed class PdfWorkerException(string message, uint pdfError) : IOException(message)
{
    public uint PdfError { get; } = pdfError;
}
public sealed class PdfReply
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public uint PdfError { get; set; }
    public int PageCount { get; set; }
    public List<PdfPageInfo>? Pages { get; set; }
    public ulong Permissions { get; set; }
    public double CropLeft { get; set; }
    public double CropTop { get; set; }
    public double CropRight { get; set; } = 1;
    public double CropBottom { get; set; } = 1;
    public string? ImagePath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Text { get; set; }
    public List<PdfCharRect>? Boxes { get; set; }
    public List<PdfOutlineItem>? Outline { get; set; }
    public Dictionary<string,string>? Metadata { get; set; }
    public int LinkPageIndex { get; set; } = -1;
    public string? LinkUrl { get; set; }
}
public sealed class PdfPageInfo { public float Width { get; set; } public float Height { get; set; } public string Label { get; set; } = ""; }
public sealed class PdfCharRect { public int Index { get; set; } public double Left { get; set; } public double Right { get; set; } public double Bottom { get; set; } public double Top { get; set; } }
public sealed class PdfOutlineItem { public string Title { get; set; } = ""; public int PageIndex { get; set; } public List<PdfOutlineItem> Children { get; set; } = new(); }
