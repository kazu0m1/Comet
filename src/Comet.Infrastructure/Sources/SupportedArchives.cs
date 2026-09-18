namespace Comet.Infrastructure.Sources;

public static class SupportedArchives
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".cbz", ".rar", ".cbr", ".7z", ".cb7"
    };

    public static bool IsSupported(string path)
        => Extensions.Contains(Path.GetExtension(path));

    public static bool UsesSystemZip(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase);
    }

    public static bool UsesSharpCompress(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".rar", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cbr", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".7z", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cb7", StringComparison.OrdinalIgnoreCase);
    }
}
