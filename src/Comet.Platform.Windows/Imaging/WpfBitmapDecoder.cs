using System.Windows.Media.Imaging;
using Comet.Core.Models;

namespace Comet.Platform.Windows.Imaging;

public sealed class WpfBitmapDecoder
{
    public PixelSize Probe(ReadOnlyMemory<byte> bytes)
    {
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        return new PixelSize(frame.PixelWidth, frame.PixelHeight);
    }

    public BitmapSource Decode(ReadOnlyMemory<byte> bytes, int? targetPixelWidth = null)
    {
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
        if (targetPixelWidth is > 0)
            image.DecodePixelWidth = targetPixelWidth.Value;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
