using GreenBoy.memory.cart.rtc;
using NUnit.Framework;
using System;

namespace GreenBoy.Test.Unit.Memory.Cart.Rtc
{
    [TestFixture, Parallelizable(ParallelScope.None)]
    public class RealTimeClockTest
    {
        private RealTimeClock _rtc;
        private VirtualClock _clock;

        [SetUp]
        public void SetUp()
        {
            _clock = new VirtualClock();
            _rtc = new RealTimeClock(_clock);
        }

        [Test]
        public void TestBasicGet()
        {
            _clock.Forward(new TimeSpan(5, 8, 12, 2));
            AssertClockEquals(5, 8, 12, 2);
        }

        [Test]
        public void TestLatch()
        {
            _clock.Forward(new TimeSpan(5, 8, 12, 2));

            _rtc.Latch();
            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(5, 8, 12, 2);

            _rtc.Unlatch();

            AssertClockEquals(5 + 10, 8 + 5, 12 + 19, 2 + 4);
        }

        [Test]
        public void TestCounterOverflow()
        {
            _clock.Forward(new TimeSpan(511, 23, 59, 59));

            Assert.That(_rtc.IsCounterOverflow(), Is.False);

            _clock.Forward(TimeSpan.FromSeconds(1));

            AssertClockEquals(0, 0, 0, 0);
            Assert.That(_rtc.IsCounterOverflow(), Is.True);

            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(10, 5, 19, 4);
            Assert.That(_rtc.IsCounterOverflow(), Is.True);

            _rtc.ClearCounterOverflow();

            AssertClockEquals(10, 5, 19, 4);
            Assert.That(_rtc.IsCounterOverflow(), Is.False);
        }

        [Test]
        public void SetClock()
        {
            _clock.Forward(new TimeSpan(10, 5, 19, 4));

            AssertClockEquals(10, 5, 19, 4);

            _rtc.SetHalt(true);

            Assert.That(_rtc.IsHalt(), Is.True);

            _rtc.SetDayCounter(10);
            _rtc.SetHours(16);
            _rtc.SetMinutes(21);
            _rtc.SetSeconds(32);

            _clock.Forward(new TimeSpan(1, 1, 1, 1)); // should be ignored after unhalt
            _rtc.SetHalt(false);

            Assert.That(_rtc.IsHalt(), Is.False);

            AssertClockEquals(10, 16, 21, 32);

            _clock.Forward(new TimeSpan(2, 2, 2, 2));

            AssertClockEquals(12, 18, 23, 34);
        }

        private void AssertClockEquals(int days, int hours, int minutes, int seconds)
        {
            Assert.That(_rtc.GetDayCounter(), Is.EqualTo(days));
            Assert.That(_rtc.GetHours(), Is.EqualTo(hours));
            Assert.That(_rtc.GetMinutes(), Is.EqualTo(minutes));
            Assert.That(_rtc.GetSeconds(), Is.EqualTo(seconds));
        }
    }
}