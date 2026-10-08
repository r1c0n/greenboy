using System;
using System.Threading;
using System.Threading.Tasks;
using GreenBoy.gui;
using NUnit.Framework;
using SkiaSharp;

namespace GreenBoy.Test.Unit.GUI
{
    public class BitmapDisplayLifecycleTest
    {
        [Test]
        public void CancellationReleasesARefreshWaitEvenWithoutADisplayWorker()
        {
            var display = new BitmapDisplay();
            display.RequestRefresh();
            using var cancellation = new CancellationTokenSource();
            var wait = Task.Run(() => display.WaitForRefresh(cancellation.Token));

            cancellation.Cancel();

            Assert.That(wait.Wait(3000), Is.True);
        }

        [Test]
        public void WorkerPreservesPendingFramesAndClearsRefreshWhenStopped()
        {
            var display = new BitmapDisplay();
            using var frameProduced = new ManualResetEventSlim();
            using var cancellation = new CancellationTokenSource();
            display.OnFrameProduced += (_, __) => frameProduced.Set();
            display.RequestRefresh();
            var worker = Task.Run(() => display.Run(cancellation.Token));
            try
            {
                Assert.That(frameProduced.Wait(3000), Is.True);
            }
            finally
            {
                cancellation.Cancel();
                Assert.That(worker.Wait(3000), Is.True);
            }

            Assert.That(display.Enabled, Is.False);
            Assert.That(Task.Run(display.WaitForRefresh).Wait(3000), Is.True);
        }

        [Test]
        public void FailureWhileProducingAFrameStillReleasesRefreshWaiters()
        {
            var display = new BitmapDisplay();
            display.OnFrameProduced += (_, __) => throw new InvalidOperationException("Frame failed");
            display.RequestRefresh();

            Assert.Throws<InvalidOperationException>(() => display.Run(CancellationToken.None));
            Assert.That(display.Enabled, Is.False);
            Assert.That(Task.Run(display.WaitForRefresh).Wait(3000), Is.True);
        }

        [Test]
        public void ResetDropsThePreviousRomsPartialFrameAndPendingRefresh()
        {
            var display = new BitmapDisplay();
            display.PutDmgPixel(3);
            display.RequestRefresh();

            display.Reset();

            Assert.That(Task.Run(display.WaitForRefresh).Wait(3000), Is.True);
            using var cancellation = new CancellationTokenSource();
            byte[] frame = null;
            display.OnFrameProduced += (_, bytes) => { frame = bytes; cancellation.Cancel(); };
            display.PutDmgPixel(1);
            display.RequestRefresh();
            display.Run(cancellation.Token);
            using var bitmap = SKBitmap.Decode(frame);
            Assert.That(bitmap.GetPixel(0, 0), Is.EqualTo(new SKColor(0x99, 0xc8, 0x86)));
            Assert.That(bitmap.GetPixel(1, 0), Is.EqualTo(SKColors.Black));
        }
    }
}
