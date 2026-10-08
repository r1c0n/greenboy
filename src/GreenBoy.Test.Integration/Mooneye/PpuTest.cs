using GreenBoy.Test.Integration.Support;
using NUnit.Framework;
using System.IO;

namespace GreenBoy.Test.Integration.Mooneye
{
    [Ignore("JVMFailed")]
    [TestFixture, Timeout(1000 * 60)]
    public class PpuTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("mooneye/acceptance/ppu");

        [Test, Parallelizable(ParallelScope.All)]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            // All but the final test fails in the JVM

            var rom = new FileInfo(filePath);
            RomTestUtils.testMooneyeRom(rom);
        }
    }
}