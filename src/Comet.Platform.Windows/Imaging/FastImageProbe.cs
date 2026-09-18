using System.Buffers.Binary;
using Comet.Core.Models;

namespace Comet.Platform.Windows.Imaging;

public static class FastImageProbe
{
    public static bool TryGetPixelSize(ReadOnlySpan<byte> bytes, out PixelSize size)
    {
        if (TryJpeg(bytes, out size)) return true;
        if (TryPng(bytes, out size)) return true;
        if (TryGif(bytes, out size)) return true;
        if (TryBmp(bytes, out size)) return true;
        size = default;
        return false;
    }

    private static bool TryJpeg(ReadOnlySpan<byte> bytes, out PixelSize size)
    {
        size = default;
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            return false;

        var i = 2;
        while (i + 3 < bytes.Length)
        {
            while (i < bytes.Length && bytes[i] != 0xFF) i++;
            while (i < bytes.Length && bytes[i] == 0xFF) i++;
            if (i >= bytes.Length) return false;

            var marker = bytes[i++];
            if (marker is 0xD8 or 0xD9 || marker == 0x01 || marker is >= 0xD0 and <= 0xD7)
                continue;

            if (i + 1 >= bytes.Length) return false;
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(i, 2));
            if (segmentLength < 2 || i + segmentLength > bytes.Length)
                return false;

            if (IsStartOfFrame(marker))
            {
                if (segmentLength < 7) return false;
                var height = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(i + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(i + 5, 2));
                if (width == 0 || height == 0) return false;
                size = new PixelSize(width, height);
                return true;
            }

            i += segmentLength;
        }

        return false;
    }

    private static bool IsStartOfFrame(byte marker)
        => marker is 0xC0 or 0xC1 or 0xC2 or 0xC3
            or 0xC5 or 0xC6 or 0xC7
            or 0xC9 or 0xCA or 0xCB
            or 0xCD or 0xCE or 0xCF;

    private static bool TryPng(ReadOnlySpan<byte> bytes, out PixelSize size)
    {
        size = default;
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < 24 || !bytes[..8].SequenceEqual(signature))
            return false;

        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4));
        if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
            return false;
        size = new PixelSize((int)width, (int)height);
        return true;
    }

    private static bool TryGif(ReadOnlySpan<byte> bytes, out PixelSize size)
    {
        size = default;
        if (bytes.Length < 10
            || bytes[0] != (byte)'G' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F'
            || bytes[3] != (byte)'8' || (bytes[4] != (byte)'7' && bytes[4] != (byte)'9') || bytes[5] != (byte)'a')
            return false;

        var width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(6, 2));
        var height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(8, 2));
        if (width == 0 || height == 0) return false;
        size = new PixelSize(width, height);
        return true;
    }

    private static bool TryBmp(ReadOnlySpan<byte> bytes, out PixelSize size)
    {
        size = default;
        if (bytes.Length < 26 || bytes[0] != (byte)'B' || bytes[1] != (byte)'M')
            return false;

        var dibSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(14, 4));
        if (dibSize == 12)
        {
            var width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(18, 2));
            var height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(20, 2));
            if (width == 0 || height == 0) return false;
            size = new PixelSize(width, height);
            return true;
        }

        if (dibSize >= 40)
        {
            var width = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(18, 4));
            var height = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(22, 4));
            if (width <= 0 || height == 0 || height == int.MinValue) return false;
            size = new PixelSize(width, Math.Abs(height));
            return true;
        }

        return false;
    }
}
