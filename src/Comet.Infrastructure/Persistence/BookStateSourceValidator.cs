using Comet.Core.Models;

namespace Comet.Infrastructure.Persistence;

public static class BookStateSourceValidator
{
    public static bool Matches(BookState state, string sourcePath)
    {
        try
        {
            if (File.Exists(sourcePath))
            {
                var info = new FileInfo(sourcePath);
                return state.SourceLength == info.Length
                    && SameTimestamp(state.SourceLastWriteTimeUtc, info.LastWriteTimeUtc);
            }

            if (Directory.Exists(sourcePath))
            {
                var info = new DirectoryInfo(sourcePath);
                return state.SourceLength == 0
                    && SameTimestamp(state.SourceLastWriteTimeUtc, info.LastWriteTimeUtc);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    private static bool SameTimestamp(DateTimeOffset saved, DateTime currentUtc)
    {
        return saved.UtcDateTime == DateTime.SpecifyKind(currentUtc, DateTimeKind.Utc);
    }
}
