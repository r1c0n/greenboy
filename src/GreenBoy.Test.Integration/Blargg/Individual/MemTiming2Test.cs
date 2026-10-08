using GreenBoy.Test.Integration.Support;
using NUnit.Framework;
using System.IO;

namespace GreenBoy.Test.Integration.Blargg.Individual
{
    [TestFixture, CancelAfter(1000 * 60 * 1)]
    public class MemTiming2Test
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("blargg/mem_timing-2");

        [Test]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testRomWithMemory(rom);
        }
    }
}