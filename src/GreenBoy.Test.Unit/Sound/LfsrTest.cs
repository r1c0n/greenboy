using GreenBoy.sound;
using NUnit.Framework;

namespace GreenBoy.Test.Unit.Sound
{
    [TestFixture]
    public class LfsrTest
    {
        [Test]
        public void testLfsr()
        {
            Lfsr lfsr = new Lfsr();
            int previousValue = 0;
            for (int i = 0; i < 100; i++)
            {
                lfsr.NextBit(false);
                Assert.That(lfsr.Value, Is.Not.EqualTo(previousValue));
                previousValue = lfsr.Value;
            }
        }

        [Test]
        public void testLfsrWidth7()
        {
            Lfsr lfsr = new Lfsr();
            int previousValue = 0;
            for (int i = 0; i < 100; i++)
            {
                lfsr.NextBit(true);
                Assert.That(lfsr.Value, Is.Not.EqualTo(previousValue));
                previousValue = lfsr.Value;
            }
        }
    }
}