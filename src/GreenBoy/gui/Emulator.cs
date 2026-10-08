using GreenBoy.controller;
using GreenBoy.gpu;
using GreenBoy.memory.cart;
using GreenBoy.serial;
using GreenBoy.sound;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GreenBoy.gui
{
    public class Emulator : IRunnable, IDisposable
    {
        public Gameboy Gameboy { get; private set; }
        public IDisplay Display { get; set; } = new BitmapDisplay();
        public IController Controller { get; set; } = new NullController();
        public SerialEndpoint SerialEndpoint { get; set; } = new NullSerialEndpoint();
        public ISoundOutput SoundOutput { get; set; }
        public GameboyOptions Options { get; set; }
        public bool Active => Array.Exists(Volatile.Read(ref _runnables), thread => thread.IsAlive);

        public Exception LastError => Volatile.Read(ref _lastError);

        private readonly object _lifecycleLock = new object();
        private Thread[] _runnables = Array.Empty<Thread>();
        private CancellationTokenSource _cancellation;
        private Exception _lastError;
        private bool _disposed;

        public Emulator(GameboyOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Run(CancellationToken token)
        {
            EnsureHostThread();
            lock (_lifecycleLock)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Emulator));
                Options.Verify();
                if (!Options.RomSpecified || !Options.RomFile.Exists)
                    throw new ArgumentException("The ROM path does not exist: " + Options.Rom);
                token.ThrowIfCancellationRequested();

                // Load first so a bad selection leaves the current ROM running.
                var rom = new Cartridge(Options);
                StopWorkers();
                _lastError = null;
                var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                _cancellation = cancellation;

                try
                {
                    var gameboy = CreateGameboy(rom, cancellation.Token);
                    Gameboy = gameboy;
                    var workers = new List<Thread>();
                    if (!Options.Headless)
                    {
                        var display = Display;
                        workers.Add(CreateWorker("GreenBoy display", () => display.Run(cancellation.Token), cancellation, false));
                    }

                    workers.Add(CreateWorker("GreenBoy emulation", () => gameboy.Run(cancellation.Token), cancellation, true));
                    _runnables = workers.ToArray();
                    foreach (var thread in _runnables) thread.Start();
                }
                catch
                {
                    StopWorkers();
                    throw;
                }
            }
        }

        private Thread CreateWorker(string name, Action run, CancellationTokenSource cancellation, bool stopPeers)
        {
            return new Thread(() =>
            {
                try
                {
                    run();
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    Interlocked.CompareExchange(ref _lastError, exception, null);
                    stopPeers = true;
                }
                finally
                {
                    if (stopPeers) cancellation.Cancel();
                }
            })
            {
                Name = name,
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
        }

        public void Stop()
        {
            EnsureHostThread();
            lock (_lifecycleLock) StopWorkers();
        }

        public void Stop(CancellationTokenSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.Cancel();
            Stop();
        }

        private void StopWorkers()
        {
            _cancellation?.Cancel();
            foreach (var thread in _runnables)
            {
                if ((thread.ThreadState & ThreadState.Unstarted) == 0 && !thread.Join(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException($"{thread.Name} did not stop. A new ROM cannot start until it exits.");
            }
            _runnables = Array.Empty<Thread>();
            _cancellation?.Dispose();
            _cancellation = null;
        }

        public void Dispose()
        {
            EnsureHostThread();
            lock (_lifecycleLock)
            {
                if (_disposed) return;
                StopWorkers();
                _disposed = true;
            }
        }

        private void EnsureHostThread()
        {
            if (Array.IndexOf(Volatile.Read(ref _runnables), Thread.CurrentThread) >= 0)
                throw new InvalidOperationException("Emulator lifecycle methods must be called from outside its workers.");
        }

        public void TogglePause()
        {
            if (Gameboy != null)
                Gameboy.Pause = !Gameboy.Pause;
        }

        private Gameboy CreateGameboy(Cartridge rom, CancellationToken token)
        {
            if (Options.Headless)
            {
                return new Gameboy(Options, rom, new NullDisplay(), new NullController(), new NullSoundOutput(), new NullSerialEndpoint());
            }

            var sound = SoundOutput;
            if (sound == null)
                sound = OperatingSystem.IsWindows() ? (ISoundOutput)new WinSound(token) : new NullSoundOutput();
            return new Gameboy(Options, rom, Display, Controller, sound, SerialEndpoint);
        }
    }
}
