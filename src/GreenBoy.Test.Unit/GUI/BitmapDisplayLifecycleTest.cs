using System;
using System.Threading;
using System.Threading.Tasks;
using GreenBoy.gui;
using NUnit.Framework;

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
    }
}
