using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine.Text;

namespace GreenBoy
{
    public class GameboyOptions
    {
        private string _rom;

        public FileInfo RomFile => string.IsNullOrWhiteSpace(Rom) ? null : new FileInfo(Rom);

        [Option('r', "rom", Required = false, HelpText = "ROM file.")]
        public string Rom { get => _rom ?? PositionalRoms?.FirstOrDefault(); set => _rom = value; }

        [Value(0, MetaName = "ROM", Max = 1, HelpText = "ROM file (or use --rom).")]
        public IEnumerable<string> PositionalRoms { get; set; } = Array.Empty<string>();

        [Option('d', "force-dmg", Required = false, HelpText = "Emulate classic GB (DMG).")]
        public bool ForceDmg { get; set; }

        [Option('c', "force-cgb", Required = false, HelpText = "Emulate color GB (CGB).")]
        public bool ForceCgb { get; set; }

        [Option('b', "use-bootstrap", Required = false, HelpText = "Start with the GB bootstrap.")]
        public bool UseBootstrap { get; set; }

        [Option("disable-battery-saves", Required = false, HelpText = "Disable battery saves.")]
        public bool DisableBatterySaves { get; set; }

        [Option("debug", Required = false, HelpText = "Enable debug output.")]
        public bool Debug { get; set; }

        [Option("headless", Required = false, HelpText = "Run without display, sound, or controller input.")]
        public bool Headless { get; set; }

        [Option("interactive", Required = false, HelpText = "Play on the console!")]
        public bool Interactive { get; set; }

        public bool ShowUi => !Headless;

        public bool IsSupportBatterySaves() => !DisableBatterySaves;

        public bool RomSpecified => !string.IsNullOrWhiteSpace(Rom);

        public GameboyOptions()
        {
        }

        public GameboyOptions(FileInfo romFile) : this(romFile, new string[0], new string[0])
        {
        }

        public GameboyOptions(FileInfo romFile, ICollection<string> longParameters, ICollection<string> shortParams)
        {
            Rom = romFile.FullName;
            ForceDmg = longParameters.Contains("force-dmg") || shortParams.Contains("d");
            ForceCgb = longParameters.Contains("force-cgb") || shortParams.Contains("c");

            UseBootstrap = longParameters.Contains("use-bootstrap") || shortParams.Contains("b");
            DisableBatterySaves = longParameters.Contains("disable-battery-saves") || shortParams.Contains("db");
            Debug = longParameters.Contains("debug");
            Headless = longParameters.Contains("headless");

            Verify();
        }

        public void Verify()
        {
            if (ForceDmg && ForceCgb)
            {
                throw new ArgumentException("--force-dmg and --force-cgb cannot be used together.");
            }

            if (Headless && Interactive)
                throw new ArgumentException("--headless and --interactive cannot be used together.");

            if (_rom != null && PositionalRoms.Any())
                throw new ArgumentException("Specify the ROM either as a path or with --rom, not both.");
        }

        public static void PrintUsage(TextWriter stream)
        {
            stream.WriteLine(GetHelp(ParseArguments(new[] { "--help" })));
            stream.Flush();
        }

        public static string GetHelp(ParserResult<GameboyOptions> result) =>
            HelpText.AutoBuild(result, help =>
            {
                help.Heading = "GreenBoy";
                help.Copyright = string.Empty;
                help.AdditionalNewLineAfterOption = false;
                help.AddPreOptionsLine("Usage: GreenBoy.Cli [options] ROM");
                return help;
            }).ToString();

        public static ParserResult<GameboyOptions> ParseArguments(string[] args)
        {
            using var parser = new Parser(cfg =>
            {
                cfg.AutoHelp = true;
                cfg.HelpWriter = null;
            });
            return parser.ParseArguments<GameboyOptions>(args);
        }

        public static GameboyOptions Parse(string[] args)
        {
            var result = ParseArguments(args);
            if (result is Parsed<GameboyOptions> parsed)
            {
                parsed.Value.Verify();
                return parsed.Value;
            }

            Console.Out.WriteLine(GetHelp(result));
            return null;
        }
    }
}
