using NUnit.Framework;
using System;
using System.IO;

namespace GreenBoy.Test.Integration.Support
{
    public class RomTestUtils
    {
        public static void testRomWithMemory(FileInfo romFileInfoInfo, bool trace = false)
        {
            Console.WriteLine($"\n### Running test rom {romFileInfoInfo.FullName} ###");
            var runner = new MemoryTestRunner(romFileInfoInfo, Console.Out, trace);

            var result = runner.RunTest(TestContext.CurrentContext.CancellationToken);

            Assert.That(result.GetStatus(), Is.EqualTo(0), "Non-zero return value");
        }

        public static void testRomWithSerial(FileInfo romFileInfoInfo, bool trace = false)
        {
            Console.WriteLine($"\n### Running test rom {romFileInfoInfo.FullName} ###");
            var runner = new SerialTestRunner(romFileInfoInfo, Console.Out, trace);

            var result = runner.RunTest(TestContext.CurrentContext.CancellationToken);

            Assert.That(result.Contains("Passed"), Is.True);
        }

        public static void testMooneyeRom(FileInfo romFileInfoInfo, bool trace = false)
        {
            Console.WriteLine($"\n### Running test rom {romFileInfoInfo.FullName} ###");
            var runner = new MooneyeTestRunner(romFileInfoInfo, Console.Out, trace);
            var result = runner.RunTest(TestContext.CurrentContext.CancellationToken);
            Assert.That(result, Is.True);
        }
    }
}
