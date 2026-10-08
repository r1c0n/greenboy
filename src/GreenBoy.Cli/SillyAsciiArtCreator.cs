using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;

namespace GreenBoy.Cli
{
    public class SillyAsciiArtCreator
    {
        private static readonly Dictionary<float, string> Map;

        static SillyAsciiArtCreator()
        {
            Map = new Dictionary<float, string>
            {
                {200, " "},
                {190, " "},
                {180, " "},
                {170, " "},
                {160, " "},
                {150, "."},
                {140, "o"},
                {130, "O"},
                {120, "+"},
                {110, "#"},
                {100, "@"},
                {080, "%"},
                {060, "░"},
                {040, "▒"},
                {020, "▓"},
                {000, "█"},
            };
        }

        public static string GenerateArt(byte[] jpg)
        {
            using var source = SKBitmap.Decode(jpg);
            using var image = source.Resize(new SKImageInfo(106, 72),
                new SKSamplingOptions(SKCubicResampler.Mitchell));

            var map = Map.OrderBy(kvp => kvp.Key).ToList();
            var sb = new StringBuilder();

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    var currentChar = MapToAscii(map, pixel);

                    sb.Append(currentChar);
                }

                sb.Append(Environment.NewLine);
            }

            return PostProcessOutput(sb);
        }

        private static string MapToAscii(IEnumerable<KeyValuePair<float, string>> map, SKColor pixel)
        {
            var currentChar = "";
            foreach (var (key, value) in map)
            {
                if (key <= pixel.Red)
                {
                    currentChar = value;
                }
            }
            return currentChar;
        }

        private static string PostProcessOutput(StringBuilder sb)
        {
            var output = sb.ToString();
            sb.Clear();
            output = output.Substring(0, output.Length - Environment.NewLine.Length);
            return output;
        }
    }
}
