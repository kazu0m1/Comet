using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Comet.Core.Models;
using SkiaSharp;

namespace Comet.Platform.Windows.Imaging;

public sealed class WpfBitmapDecoder
{
    public PixelSize Probe(ReadOnlyMemory<byte> bytes)
    {
        if (FastImageProbe.TryGetPixelSize(bytes.Span, out var fastSize))
            return fastSize;

        if (IsWebP(bytes.Span))
        {
            using var bitmap = SKBitmap.Decode(bytes.ToArray())
                ?? throw new InvalidDataException("Unable to decode WebP image.");
            return new PixelSize(bitmap.Width, bitmap.Height);
        }

        using var stream = OpenReadOnlyStream(bytes);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnDemand);
        var frame = decoder.Frames[0];
        return new PixelSize(frame.PixelWidth, frame.PixelHeight);
    }

    public BitmapSource Decode(ReadOnlyMemory<byte> bytes, int? targetPixelWidth = null)
    {
        if (IsWebP(bytes.Span))
            return DecodeWebP(bytes, targetPixelWidth);

        using var stream = OpenReadOnlyStream(bytes);
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

    private static BitmapSource DecodeWebP(ReadOnlyMemory<byte> bytes, int? targetPixelWidth)
    {
        using var decoded = SKBitmap.Decode(bytes.ToArray())
            ?? throw new InvalidDataException("Unable to decode WebP image.");

        SKBitmap? resized = null;
        SKBitmap source = decoded;

        if (targetPixelWidth is > 0 && decoded.Width > targetPixelWidth.Value)
        {
            var width = targetPixelWidth.Value;
            var height = Math.Max(1, (int)Math.Round(decoded.Height * (width / (double)decoded.Width)));
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            resized = decoded.Resize(info, new SKSamplingOptions(SKFilterMode.Linear));
            if (resized is not null)
                source = resized;
        }

        SKBitmap? converted = null;
        if (source.ColorType != SKColorType.Bgra8888 || source.AlphaType != SKAlphaType.Premul)
        {
            var info = new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            converted = new SKBitmap(info);
            if (!source.CopyTo(converted, SKColorType.Bgra8888))
            {
                converted.Dispose();
                resized?.Dispose();
                throw new InvalidDataException("Unable to convert WebP pixels.");
            }
            source = converted;
        }

        try
        {
            var bitmap = BitmapSource.Create(
                source.Width,
                source.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                source.GetPixels(),
                checked(source.RowBytes * source.Height),
                source.RowBytes);
            bitmap.Freeze();
            return bitmap;
        }
        finally
        {
            converted?.Dispose();
            resized?.Dispose();
        }
    }

    private static MemoryStream OpenReadOnlyStream(ReadOnlyMemory<byte> bytes)
    {
        if (MemoryMarshal.TryGetArray(bytes, out ArraySegment<byte> segment) && segment.Array is not null)
            return new MemoryStream(segment.Array, segment.Offset, segment.Count, writable: false, publiclyVisible: true);

        return new MemoryStream(bytes.ToArray(), writable: false);
    }

    private static bool IsWebP(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 12
            && bytes[0] == (byte)'R'
            && bytes[1] == (byte)'I'
            && bytes[2] == (byte)'F'
            && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W'
            && bytes[9] == (byte)'E'
            && bytes[10] == (byte)'B'
            && bytes[11] == (byte)'P';
    }
}
