using System.Text.Json;
using Comet.Core.Abstractions;
using Comet.Core.Models;

namespace Comet.Infrastructure.Persistence;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public JsonSettingsStore(string? baseDirectory = null)
    {
        var root = baseDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Comet");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "settings.json");
    }

    public async ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_path)) return new AppSettings();

            var json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();

            // v0.1.0 initially persisted ShowThumbnails=false before the sidebar
            // existed. Schema 2 also adopts a compact MComix-like thumbnail width.
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(AppSettings.SettingsSchemaVersion), out _))
            {
                settings = settings with
                {
                    SettingsSchemaVersion = 2,
                    ShowThumbnails = true,
                    ThumbnailWidth = 72
                };
            }
            else if (settings.SettingsSchemaVersion < 2)
            {
                settings = settings with
                {
                    SettingsSchemaVersion = 2,
                    ThumbnailWidth = 72
                };
            }

            return settings;
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public async ValueTask SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var temp = _path + ".tmp";
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, settings, Options, cancellationToken).ConfigureAwait(false);
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            _writeGate.Release();
        }
    }
}
