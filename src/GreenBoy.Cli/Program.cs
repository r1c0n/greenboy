using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            if (arguments.Interactive && (Console.IsInputRedirected || Console.IsOutputRedirected))
            {
                error.WriteLine("Error: --interactive requires a terminal with console input and output.");
                return 2;
            }

            try
            {
                using var cancellation = new CancellationTokenSource();
                using var emulator = new Emulator(arguments);
                ConsoleCancelEventHandler onCancel = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cancellation.Cancel();
                };
                Console.CancelKeyPress += onCancel;
                try
                {
                    if (arguments.Interactive)
                    {
                        var ui = new CommandLineInteractivity();
                        emulator.Controller = ui;
                        emulator.Display.OnFrameProduced += ui.UpdateDisplay;
                        emulator.Run(cancellation.Token);
                        ui.ProcessInput(cancellation.Token, () => emulator.Active);
                    }
                    else
                    {
                        emulator.Run(cancellation.Token);
                        output.WriteLine("Running headless.");
                        output.WriteLine(Console.IsInputRedirected ? "Press Ctrl+C to exit." : "Press ANY key or Ctrl+C to exit.");
                        while (!cancellation.IsCancellationRequested && emulator.Active)
                        {
                            if (!Console.IsInputRedirected && Console.KeyAvailable)
                            {
                                Console.ReadKey(true);
                                break;
                            }
                            cancellation.Token.WaitHandle.WaitOne(25);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                }
                finally
                {
                    Console.CancelKeyPress -= onCancel;
                    cancellation.Cancel();
                }

                emulator.Stop();
                if (emulator.LastError != null)
                {
                    error.WriteLine($"Error: {emulator.LastError.Message}");
                    return 1;
                }
                return 0;
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException ||
                exception is ArgumentException || exception is InvalidOperationException || exception is TimeoutException)
            {
                error.WriteLine($"Error: {exception.Message}");
                return 1;
            }
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

        public void ProcessInput(CancellationToken token = default, Func<bool> isRunning = null)
        {
            Button lastButton = null;
            long releaseAt = 0;
            try
            {
                while (!token.IsCancellationRequested && (isRunning?.Invoke() ?? true))
                {
                    if (lastButton != null && Stopwatch.GetTimestamp() >= releaseAt)
                    {
                        _listener?.OnButtonRelease(lastButton);
                        lastButton = null;
                    }

                    if (!Console.KeyAvailable)
                    {
                        token.WaitHandle.WaitOne(25);
                        continue;
                    }
                    var input = Console.ReadKey(true);
                    if (input.Key == ConsoleKey.Escape) break;
                    if (!_controls.TryGetValue(input.Key, out var button)) continue;
                    if (lastButton != null && lastButton != button)
                        _listener?.OnButtonRelease(lastButton);
                    _listener?.OnButtonPress(button);
                    lastButton = button;
                    releaseAt = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 2;
                }
            }
            finally
            {
                if (lastButton != null) _listener?.OnButtonRelease(lastButton);
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
