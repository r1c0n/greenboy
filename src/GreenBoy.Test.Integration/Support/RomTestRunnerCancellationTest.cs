using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace GreenBoy.Test.Integration.Support
{
    public class RomTestRunnerCancellationTest
    {
        [TestCase("memory")]
        [TestCase("serial")]
        [TestCase("mooneye")]
        public void RunnerObservesCancellationDuringEmulation(string runnerType)
        {
            // A ROM that loops without either test suite's completion signature.
            var romPath = Path.GetTempFileName();
            try
            {
                var romBytes = new byte[0x8000];
                romBytes[0x100] = 0xc3; // JP 0x0104
                romBytes[0x101] = 0x04;
                romBytes[0x102] = 0x01;
                romBytes[0x104] = 0xc3; // JP 0x0100
                romBytes[0x105] = 0x00;
                romBytes[0x106] = 0x01;
                File.WriteAllBytes(romPath, romBytes);
                var rom = new FileInfo(romPath);
                using var cancellation = new CancellationTokenSource();
                Action run;
                switch (runnerType)
                {
                    case "memory":
                        var memoryRunner = new MemoryTestRunner(rom, TextWriter.Null);
                        run = () => memoryRunner.RunTest(cancellation.Token);
                        break;
                    case "serial":
                        var serialRunner = new SerialTestRunner(rom, TextWriter.Null, false);
                        run = () => serialRunner.RunTest(cancellation.Token);
                        break;
                    default:
                        var mooneyeRunner = new MooneyeTestRunner(rom, TextWriter.Null, false);
                        run = () => mooneyeRunner.RunTest(cancellation.Token);
                        break;
                }

                cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));
                Assert.That(() => run(), Throws.InstanceOf<OperationCanceledException>());
            }
            finally
            {
                File.Delete(romPath);
            }
        }
    }
}
