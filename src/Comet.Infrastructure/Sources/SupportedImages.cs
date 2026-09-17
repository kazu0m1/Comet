namespace Comet.Infrastructure.Sources;

public static class SupportedImages
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"
    };

    public static bool IsSupported(string name) => Extensions.Contains(Path.GetExtension(name));
}
