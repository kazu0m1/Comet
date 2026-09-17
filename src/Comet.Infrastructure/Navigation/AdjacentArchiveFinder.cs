using Comet.Core.Services;

namespace Comet.Infrastructure.Navigation;

public sealed class AdjacentArchiveFinder
{
    public string? FindNext(string currentPath) => Find(currentPath, +1);
    public string? FindPrevious(string currentPath) => Find(currentPath, -1);

    private static string? Find(string currentPath, int direction)
    {
        if (Directory.Exists(currentPath)) return null;
        var full = Path.GetFullPath(currentPath);
        var directory = Path.GetDirectoryName(full);
        if (directory is null || !Directory.Exists(directory)) return null;

        var archives = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(p => Path.GetExtension(p).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                     || Path.GetExtension(p).Equals(".cbz", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => Path.GetFileName(p), NaturalStringComparer.Instance)
            .ToArray();

        var index = Array.FindIndex(archives, p => string.Equals(Path.GetFullPath(p), full, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        var target = index + direction;
        return target >= 0 && target < archives.Length ? archives[target] : null;
    }
}
