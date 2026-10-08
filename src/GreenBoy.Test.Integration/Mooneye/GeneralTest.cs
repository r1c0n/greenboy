using GreenBoy.Test.Integration.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace GreenBoy.Test.Integration.Mooneye
{
    [TestFixture, CancelAfter(1000 * 60)]
    public class GeneralTest
    {
        // Keep the current boot ROM failures visible without weakening the other CI checks.
        // Remove a ROM from this list when its emulation failure is fixed.
        private static readonly HashSet<string> KnownBootFailureRoms = new HashSet<string>(StringComparer.Ordinal)
        {
            "boot_div-dmgABCmgb.gb",
            "boot_div-dmg0.gb",
            "boot_hwio-dmgABCmgb.gb",
            "boot_hwio-dmg0.gb",
            "boot_regs-dmg0.gb"
        };

        public static IEnumerable<TestCaseData> RomsFrom
        {
            get
            {
                foreach (var parameters in ParametersProvider.getParameters("mooneye/acceptance"))
                {
                    var filePath = (string)parameters[0];
                    var testCase = new TestCaseData(filePath);
                    if (KnownBootFailureRoms.Contains(Path.GetFileName(filePath)))
                    {
                        testCase.SetCategory("KnownBootFailure");
                    }

                    yield return testCase;
                }
            }
        }

        [Test, Parallelizable(ParallelScope.All)]
        [TestCaseSource(nameof(RomsFrom))]
        public void Execute(string filePath)
        {
            var rom = new FileInfo(filePath);
            RomTestUtils.testMooneyeRom(rom);
        }
    }
}
