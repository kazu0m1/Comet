using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Comet.App.Services;

public static class PerformanceTrace
{
    private static readonly bool Enabled =
        string.Equals(Environment.GetEnvironmentVariable("COMET_PERF"), "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("COMET_PERF"), "true", StringComparison.OrdinalIgnoreCase);

    private static readonly object Gate = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Comet",
        "logs",
        "performance.log");

    public static long Start() => Enabled ? Stopwatch.GetTimestamp() : 0;

    public static void Elapsed(string operation, long startedAt, string? detail = null)
    {
        if (!Enabled || startedAt == 0)
            return;

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        Write(operation, elapsed.TotalMilliseconds, detail);
    }

    public static void Event(string operation, string? detail = null)
    {
        if (!Enabled)
            return;

        Write(operation, null, detail);
    }

    private static void Write(string operation, double? milliseconds, string? detail)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(directory);
            var line = string.Create(
                CultureInfo.InvariantCulture,
                $"{DateTimeOffset.Now:O}\t{operation}\t{(milliseconds is null ? "-" : milliseconds.Value.ToString("0.###", CultureInfo.InvariantCulture))} ms\t{detail ?? string.Empty}{Environment.NewLine}");
            lock (Gate)
                File.AppendAllText(LogPath, line);
        }
        catch
        {
            // Diagnostics must never affect reading.
        }
    }
}
