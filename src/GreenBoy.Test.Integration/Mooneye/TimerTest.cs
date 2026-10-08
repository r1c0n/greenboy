using GreenBoy.Test.Integration.Support;
using NUnit.Framework;
using System.IO;

namespace GreenBoy.Test.Integration.Mooneye
{
    [TestFixture, CancelAfter(1000 * 60)]
    public class TimerTest
    {
        public static object[] RomsFrom => ParametersProvider.getParameters("mooneye/acceptance/timer");

        [Test, Parallelizable(ParallelScope.All)]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testMooneyeRom(rom);
        }
    }
}