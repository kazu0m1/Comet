using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Comet.Core.Abstractions;
using Comet.Core.Models;

namespace Comet.Infrastructure.Persistence;

public sealed class JsonBookStateStore : IBookStateStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public JsonBookStateStore(string? baseDirectory = null)
    {
        var root = baseDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Comet");
        _root = Path.Combine(root, "books");
        Directory.CreateDirectory(_root);
    }

    public async ValueTask<BookState?> LoadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var path = StatePath(sourcePath);
        try
        {
            if (!File.Exists(path)) return null;
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<BookState>(stream, Options, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public async ValueTask SaveAsync(string sourcePath, BookState state, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var path = StatePath(sourcePath);
        var temp = path + ".tmp";
        try
        {
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, state, Options, cancellationToken).ConfigureAwait(false);
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            TryDeleteTemp(temp);
            _writeGate.Release();
        }
    }

    private static void TryDeleteTemp(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // A stale temp file is harmless and must not make reading-state persistence fatal.
        }
    }

    private string StatePath(string sourcePath)
    {
        var normalized = Path.GetFullPath(sourcePath).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(_root, hash + ".json");
    }
}
