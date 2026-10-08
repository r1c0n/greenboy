using System;
using System.IO;
using GreenBoy.Cli;
using NUnit.Framework;

namespace GreenBoy.Test.Unit.CLI
{
    public class ArgumentsTest
    {
        [TestCase("--help")]
        [TestCase("--version")]
        public void InformationalArgumentsReturnSuccessWithoutStartingAnEmulator(string argument)
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = Program.Run(new[] { argument }, output, error);

            Assert.That(exitCode, Is.Zero);
            Assert.That(output.ToString(), Does.Contain("GreenBoy"));
            Assert.That(error.ToString(), Is.Empty);
        }

        [TestCase(new string[0], "ROM path is required")]
        [TestCase(new[] { "--unknown" }, "unknown")]
        [TestCase(new[] { "--rom" }, "ROM path is required")]
        [TestCase(new[] { "--force-dmg", "--force-cgb" }, "cannot be used together")]
        [TestCase(new[] { "--headless", "--interactive" }, "cannot be used together")]
        [TestCase(new[] { "first.gb", "second.gb" }, "ROM")]
        [TestCase(new[] { "--rom", "first.gb", "second.gb" }, "not both")]
        public void InvalidArgumentsReturnAnErrorWithoutStartingAnEmulator(string[] args, string message)
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = Program.Run(args, output, error);

            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(output.ToString(), Is.Empty);
            Assert.That(error.ToString(), Does.Contain(message));
            Assert.That(error.ToString(), Does.Not.Contain("Exception"));
        }

        [Test]
        public void MissingRomReturnsAnError()
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            var missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gb");

            Assert.That(Program.Run(new[] { missingFile }, output, error), Is.EqualTo(2));
            Assert.That(error.ToString(), Does.Contain("does not exist").And.Contain(missingFile));
            Assert.That(output.ToString(), Is.Empty);
        }

        [Test]
        public void TruncatedRomReturnsAStartupErrorWithoutAStackTrace()
        {
            var rom = Path.GetTempFileName();
            try
            {
                using var output = new StringWriter();
                using var error = new StringWriter();

                Assert.That(Program.Run(new[] { rom, "--headless" }, output, error), Is.EqualTo(1));
                Assert.That(error.ToString(), Does.Contain("cartridge header").And.Not.Contain("Exception"));
            }
            finally
            {
                File.Delete(rom);
            }
        }

        [Test]
        public void EmulationFailureStopsTheSessionAndReturnsAnError()
        {
            var romPath = Path.GetTempFileName();
            try
            {
                var rom = new byte[32 * 1024];
                rom[0x100] = 0xd3; // An undefined instruction triggers a CPU worker failure.
                File.WriteAllBytes(romPath, rom);
                using var output = new StringWriter();
                using var error = new StringWriter();

                Assert.That(Program.Run(new[] { romPath, "--headless" }, output, error), Is.EqualTo(1));
                Assert.That(error.ToString(), Does.Contain("No command for 0xD3").And.Not.Contain("Exception"));
            }
            finally
            {
                File.Delete(romPath);
            }
        }

        [TestCase(new[] { "game.gb", "--interactive" }, "game.gb")]
        [TestCase(new[] { "--interactive", "--rom", "game.gb" }, "game.gb")]
        [TestCase(new[] { "games/my game.gb", "--interactive" }, "games/my game.gb")]
        public void RomPathsCanBeCombinedWithOptions(string[] args, string rom)
        {
            var options = GameboyOptions.Parse(args);

            Assert.That(options.Rom, Is.EqualTo(rom));
            Assert.That(options.Interactive, Is.True);
            Assert.DoesNotThrow(options.Verify);
        }
    }
}
