using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace GreenBoy.gui
{
    public class GameboyDisplayFrame
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;

        private readonly int[] _pixels;

        public GameboyDisplayFrame(int[] pixels) => _pixels = pixels;

        public IEnumerable<int[]> Rows()
        {
            var offset = 0;
            for (var row = 0; row < DisplayHeight; row++)
            {
                var thisRow = new int[DisplayWidth];
                Array.Copy(_pixels, offset, thisRow, 0, 160);
                yield return thisRow;
                offset += 160;
            }
        }

        public byte[] ToBitmap()
        {
            // A 24-bit BMP uses bottom-up BGR rows, padded to four-byte boundaries.
            const int headerSize = 54;
            var stride = (DisplayWidth * 3 + 3) & ~3;
            var imageSize = stride * DisplayHeight;
            using var memoryStream = new MemoryStream(headerSize + imageSize);
            using var writer = new BinaryWriter(memoryStream);
            writer.Write((ushort)0x4d42); // BM
            writer.Write(headerSize + imageSize);
            writer.Write(0); // Reserved
            writer.Write(headerSize);
            writer.Write(40); // BITMAPINFOHEADER size
            writer.Write(DisplayWidth);
            writer.Write(DisplayHeight);
            writer.Write((ushort)1); // Color planes
            writer.Write((ushort)24); // Bits per pixel
            writer.Write(0); // Uncompressed
            writer.Write(imageSize);
            writer.Write(0); // Horizontal resolution
            writer.Write(0); // Vertical resolution
            writer.Write(0); // Palette colors
            writer.Write(0); // Important colors

            for (var y = DisplayHeight - 1; y >= 0; y--)
            {
                for (var x = 0; x < DisplayWidth; x++)
                {
                    var (r, g, b) = _pixels[y * DisplayWidth + x].ToRgb();
                    writer.Write((byte)b);
                    writer.Write((byte)g);
                    writer.Write((byte)r);
                }
                for (var padding = DisplayWidth * 3; padding < stride; padding++) writer.Write((byte)0);
            }
            return memoryStream.ToArray();
        }
    }

    public static class GameboyDisplayFrameHelperExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int, int) ToRgb(this int pixel)
        {
            var b = pixel & 255;
            var g = (pixel >> 8) & 255;
            var r = (pixel >> 16) & 255;
            return (r, g, b);
        }
    }
}
