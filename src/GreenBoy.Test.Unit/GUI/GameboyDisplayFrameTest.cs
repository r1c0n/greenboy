using GreenBoy.gui;
using NUnit.Framework;
using SkiaSharp;

namespace GreenBoy.Test.Unit.GUI
{
    public class GameboyDisplayFrameTest
    {
        [Test]
        public void BitmapPreservesDimensionsAndRgbPixels()
        {
            var pixels = new int[160 * 144];
            pixels[0] = 0x123456;
            pixels[159] = 0xff0000;
            pixels[160] = 0x00ff00;
            pixels[pixels.Length - 1] = 0x0000ff;

            var bitmap = new GameboyDisplayFrame(pixels).ToBitmap();
            using var image = SKBitmap.Decode(bitmap);

            Assert.That(image.Width, Is.EqualTo(160));
            Assert.That(image.Height, Is.EqualTo(144));
            Assert.That(image.GetPixel(0, 0), Is.EqualTo(new SKColor(0x12, 0x34, 0x56)));
            Assert.That(image.GetPixel(159, 0), Is.EqualTo(new SKColor(255, 0, 0)));
            Assert.That(image.GetPixel(0, 1), Is.EqualTo(new SKColor(0, 255, 0)));
            Assert.That(image.GetPixel(159, 143), Is.EqualTo(new SKColor(0, 0, 255)));
        }
    }
}
