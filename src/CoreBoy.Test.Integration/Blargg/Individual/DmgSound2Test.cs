using CoreBoy.Test.Integration.Support;
using NUnit.Framework;
using System.IO;

namespace CoreBoy.Test.Integration.Blargg.Individual
{
    [TestFixture, Timeout(1000 * 60 * 2)]
    public class DmgSound2Test
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("blargg/dmg_sound-2");

        [Test]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testRomWithMemory(rom);
        }
    }
}