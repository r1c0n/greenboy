using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenBoy.gpu;
using GreenBoy.gui;
using GreenBoy.sound;
using NUnit.Framework;

namespace GreenBoy.Test.Unit.GUI
{
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class EmulatorLifecycleTest
    {
        private string _romPath;
        private Emulator _emulator;
        private TrackingDisplay _display;
        private TrackingSound _sound;

        [SetUp]
        public void SetUp()
        {
            _romPath = Path.GetTempFileName();
            var rom = new byte[32 * 1024];
            rom[0x100] = 0xc3; // JP 0x0100: a ROM that runs until cancelled.
            rom[0x102] = 0x01;
            File.WriteAllBytes(_romPath, rom);
            _display = new TrackingDisplay();
            _sound = new TrackingSound();
            _emulator = new Emulator(new GameboyOptions(new FileInfo(_romPath)))
            {
                Display = _display,
                SoundOutput = _sound
            };
        }

        [TearDown]
        public void TearDown()
        {
            _emulator.Dispose();
            _display.Dispose();
            File.Delete(_romPath);
        }

        [Test]
        public void StopJoinsWorkersAndReleasesSoundWithoutCancellingTheCaller()
        {
            using var cancellation = new CancellationTokenSource();
            _display.OnStop = () => { _ = _emulator.Active; };
            _emulator.Run(cancellation.Token);
            WaitForWorkers();

            _emulator.Stop();

            AssertWorkersStopped();
            Assert.That(_sound.Stops, Is.EqualTo(1));
            Assert.That(cancellation.IsCancellationRequested, Is.False);
            Assert.That(_emulator.LastError, Is.Null);
            Assert.DoesNotThrow(_emulator.Stop);
        }

        [Test]
        public void RestartJoinsTheOldWorkersBeforeRunningAnotherRom()
        {
            var otherRom = Path.GetTempFileName();
            File.Copy(_romPath, otherRom, true);
            try
            {
                for (var run = 0; run < 6; run++)
                {
                    var oldWorkers = _display.Workers.ToArray();
                    var oldGameboy = _emulator.Gameboy;
                    _emulator.Options.Rom = run % 2 == 0 ? _romPath : otherRom;
                    _emulator.Run(CancellationToken.None);
                    WaitForWorkers();

                    Assert.That(oldWorkers.All(thread => !thread.IsAlive), Is.True);
                    Assert.That(_emulator.Gameboy, Is.Not.SameAs(oldGameboy));
                    Assert.That(_emulator.Active, Is.True);
                }
                _emulator.Stop();
                AssertWorkersStopped();
                Assert.That(_sound.Stops, Is.EqualTo(6));
            }
            finally
            {
                _emulator.Stop();
                File.Delete(otherRom);
            }
        }

        [Test]
        public void StopWakesAPausedCpuPromptly()
        {
            _emulator.Run(CancellationToken.None);
            WaitForWorkers();
            _emulator.Gameboy.Pause = true;
            var cpuThread = _display.CpuThreads.Single();
            Assert.That(SpinWait.SpinUntil(() =>
                (cpuThread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0, 3000), Is.True);

            var elapsed = Stopwatch.StartNew();
            _emulator.Stop();

            AssertWorkersStopped();
            Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(500)));
        }

        [Test]
        public void StopCancelsACpuWaitingForTheDisplay()
        {
            _display.BlockRefresh = true;
            _emulator.Run(CancellationToken.None);
            WaitForWorkers();
            Assert.That(_display.CpuWaiting.Wait(3000), Is.True);

            _emulator.Stop();

            AssertWorkersStopped();
        }

        [Test]
        public void CallerCancellationStopsBothWorkersAndAllowsRestart()
        {
            using var cancellation = new CancellationTokenSource();
            _emulator.Run(cancellation.Token);
            WaitForWorkers();
            cancellation.Cancel();

            Assert.That(SpinWait.SpinUntil(() => !_emulator.Active, 3000), Is.True);
            AssertWorkersStopped();
            _emulator.Run(CancellationToken.None);
            WaitForWorkers();
            Assert.That(_emulator.Active, Is.True);
        }

        [Test]
        public void InvalidRomSelectionLeavesTheCurrentWorkersRunning()
        {
            _emulator.Run(CancellationToken.None);
            WaitForWorkers();
            var gameboy = _emulator.Gameboy;
            _emulator.Options.Rom = _romPath + ".missing";

            Assert.Throws<ArgumentException>(() => _emulator.Run(CancellationToken.None));
            Assert.That(_emulator.Gameboy, Is.SameAs(gameboy));
            Assert.That(_emulator.Active, Is.True);
            Assert.That(_display.Workers.All(thread => thread.IsAlive), Is.True);
        }

        [Test]
        public void WorkerFailureStopsItsPeerAndIsAvailableToTheHost()
        {
            var failure = new InvalidOperationException("Display failed");
            _display.Failure = failure;
            _emulator.Run(CancellationToken.None);

            Assert.That(SpinWait.SpinUntil(() => !_emulator.Active, 3000), Is.True);
            _emulator.Stop();
            Assert.That(_emulator.LastError, Is.SameAs(failure));
            Assert.That(_sound.Stops, Is.EqualTo(1));
        }

        [Test]
        public void TruncatedRomSelectionLeavesTheCurrentGameboyRunning()
        {
            _emulator.Run(CancellationToken.None);
            WaitForWorkers();
            var gameboy = _emulator.Gameboy;
            File.WriteAllBytes(_romPath, Array.Empty<byte>());

            Assert.Throws<InvalidDataException>(() => _emulator.Run(CancellationToken.None));
            Assert.That(_emulator.Gameboy, Is.SameAs(gameboy));
            Assert.That(_emulator.Active, Is.True);
        }

        [Test]
        public void HeadlessRunStartsAWorkerAndReturnsToTheHost()
        {
            _emulator.Options.Headless = true;
            using var cancellation = new CancellationTokenSource();
            var start = Task.Run(() => _emulator.Run(cancellation.Token));
            try
            {
                Assert.That(start.Wait(3000), Is.True);
                Assert.That(_emulator.Active, Is.True);
            }
            finally
            {
                cancellation.Cancel();
                Assert.That(start.Wait(3000), Is.True);
                _emulator.Stop();
            }
            Assert.That(_emulator.Active, Is.False);
        }

        [Test]
        public void DisposeJoinsWorkersAndPreventsRestart()
        {
            _emulator.Run(CancellationToken.None);
            WaitForWorkers();

            _emulator.Dispose();

            AssertWorkersStopped();
            Assert.DoesNotThrow(_emulator.Dispose);
            Assert.Throws<ObjectDisposedException>(() => _emulator.Run(CancellationToken.None));
        }

        private void WaitForWorkers()
        {
            Assert.That(_display.DisplayStarted.WaitOne(3000), Is.True, "Display did not start");
            Assert.That(_display.CpuStarted.WaitOne(3000), Is.True, "CPU did not produce a frame");
        }

        private void AssertWorkersStopped()
        {
            Assert.That(_emulator.Active, Is.False);
            Assert.That(_display.Workers.All(thread => !thread.IsAlive), Is.True);
        }

        private sealed class TrackingSound : ISoundOutput
        {
            public int Stops;
            public void Start() { }
            public void Stop() => Interlocked.Increment(ref Stops);
            public void Play(int left, int right) { }
        }

        private sealed class TrackingDisplay : IDisplay, IDisposable
        {
            private readonly ConcurrentDictionary<Thread, byte> _cpuThreads = new ConcurrentDictionary<Thread, byte>();
            public readonly ConcurrentBag<Thread> Workers = new ConcurrentBag<Thread>();
            public readonly AutoResetEvent DisplayStarted = new AutoResetEvent(false);
            public readonly AutoResetEvent CpuStarted = new AutoResetEvent(false);
            public readonly ManualResetEventSlim CpuWaiting = new ManualResetEventSlim();
            public Thread[] CpuThreads => _cpuThreads.Keys.ToArray();
            public bool BlockRefresh;
            public Exception Failure;
            public Action OnStop;
            public bool Enabled { get; set; }
            public event FrameProducedEventHandler OnFrameProduced { add { } remove { } }
            public void PutDmgPixel(int color) { }
            public void PutColorPixel(int color) { }

            public void RequestRefresh()
            {
                if (_cpuThreads.TryAdd(Thread.CurrentThread, 0))
                {
                    Workers.Add(Thread.CurrentThread);
                    CpuStarted.Set();
                }
            }

            public void WaitForRefresh() => WaitForRefresh(CancellationToken.None);
            public void WaitForRefresh(CancellationToken token)
            {
                if (!BlockRefresh) return;
                CpuWaiting.Set();
                token.WaitHandle.WaitOne();
            }

            public void Run(CancellationToken token)
            {
                Workers.Add(Thread.CurrentThread);
                DisplayStarted.Set();
                if (Failure != null) throw Failure;
                token.WaitHandle.WaitOne();
                OnStop?.Invoke();
            }

            public void Dispose()
            {
                DisplayStarted.Dispose();
                CpuStarted.Dispose();
                CpuWaiting.Dispose();
            }
        }
    }
}
