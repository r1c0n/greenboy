using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLine;
using GreenBoy.controller;
using GreenBoy.gui;
using Button = GreenBoy.controller.Button;

namespace GreenBoy.Cli
{
    public class Program
    {
        public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

        public static int Run(string[] args, TextWriter output, TextWriter error)
        {
            var result = GameboyOptions.ParseArguments(args);
            if (result is NotParsed<GameboyOptions> notParsed)
            {
                var informational = notParsed.Errors.All(e =>
                    e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.VersionRequestedError);
                var writer = informational ? output : error;
                writer.WriteLine(notParsed.Errors.Any(e => e.Tag == ErrorType.VersionRequestedError)
                    ? $"GreenBoy {typeof(Program).Assembly.GetName().Version}"
                    : GameboyOptions.GetHelp(result));
                return informational ? 0 : 2;
            }

            var arguments = ((Parsed<GameboyOptions>)result).Value;
            try
            {
                arguments.Verify();
                if (!arguments.RomSpecified)
                    throw new ArgumentException("A ROM path is required. Use --help for usage.");
                if (!arguments.RomFile.Exists)
                    throw new ArgumentException($"The ROM path does not exist: {arguments.Rom}");
            }
            catch (ArgumentException exception)
            {
                error.WriteLine($"Error: {exception.Message}");
                return 2;
            }

            var cancellation = new CancellationTokenSource();
            var emulator = new Emulator(arguments) { Display = new BitmapDisplay() };

            if (arguments.Interactive)
            {
                var ui = new CommandLineInteractivity();
                emulator.Controller = ui;
                emulator.Display.OnFrameProduced += ui.UpdateDisplay; 
                emulator.Run(cancellation.Token);
                ui.ProcessInput();
            }
            else
            {
                emulator.Run(cancellation.Token);
                Console.WriteLine("Running headless.");
                Console.WriteLine("Press ANY key to exit.");
                Console.ReadKey(true);
            }

            cancellation.Cancel();
            return 0;
        }
    }

    public class CommandLineInteractivity : IController
    {
        private IButtonListener _listener;
        private readonly Dictionary<ConsoleKey, Button> _controls;

        public CommandLineInteractivity()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            if (OperatingSystem.IsWindows())
            {
                Console.WindowHeight = 92;
            }

            _controls = new Dictionary<ConsoleKey, Button>
            {
                {ConsoleKey.LeftArrow, Button.Left},
                {ConsoleKey.RightArrow, Button.Right},
                {ConsoleKey.UpArrow, Button.Up},
                {ConsoleKey.DownArrow, Button.Down},
                {ConsoleKey.Z, Button.A},
                {ConsoleKey.X, Button.B},
                {ConsoleKey.Enter, Button.Start},
                {ConsoleKey.Backspace, Button.Select}
            };
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        // Should probably be called "try to process input" amirite
        // ☜(ﾟヮﾟ☜)  (❁´◡`❁)  ( •_•)>⌐■-■
        public void ProcessInput()
        {
            Button lastButton = null;
            var input = Console.ReadKey(true);
            while (input.Key != ConsoleKey.Escape)
            {
                var button = _controls.ContainsKey(input.Key) ? _controls[input.Key] : null;

                if (button != null)
                {
                    if (lastButton != button)
                    {
                        _listener?.OnButtonRelease(lastButton);
                    }

                    _listener?.OnButtonPress(button);

                    var snapshot = button;
                    new Thread(() =>
                    {
                        Thread.Sleep(500);
                        _listener?.OnButtonRelease(snapshot);
                    }).Start(); // Yo dawn, I hear you like threads.

                    lastButton = button;
                }

                input = Console.ReadKey(true);
            }
        }

        public void UpdateDisplay(object sender, byte[] framedata)
        {
            var frame = SillyAsciiArtCreator.GenerateArt(framedata);
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(frame);
        }
    }
}
