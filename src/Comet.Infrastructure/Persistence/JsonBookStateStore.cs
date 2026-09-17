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
    }

    public async ValueTask SaveAsync(string sourcePath, BookState state, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = StatePath(sourcePath);
            var temp = path + ".tmp";
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, state, Options, cancellationToken).ConfigureAwait(false);
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private string StatePath(string sourcePath)
    {
        var normalized = Path.GetFullPath(sourcePath).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(_root, hash + ".json");
    }
}
