using GreenBoy.gpu;
using GreenBoy.memory;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace GreenBoy.Test.Unit.GPU
{
    [TestFixture, Parallelizable(ParallelScope.None)]
    public class PixelFifoTest
    {
        private DmgPixelFifo _fifo;

        [SetUp]
        public void SetUp()
        {
            var registers = new MemoryRegisters(GpuRegister.Values().ToArray());
            registers.Put(GpuRegister.Bgp, 0b11100100);
            _fifo = new DmgPixelFifo(new NullDisplay(), registers);
        }

        [Test]
        public void TestEnqueue()
        {
            _fifo.Enqueue8Pixels(Zip(0b11001001, 0b11110000, false), TileAttributes.Empty);
            Assert.That(ArrayQueueAsList(_fifo.Pixels), Is.EqualTo(new List<int> { 3, 3, 2, 2, 1, 0, 0, 1 }));
        }

        [Test]
        public void TestDequeue()
        {
            _fifo.Enqueue8Pixels(Zip(0b11001001, 0b11110000, false), TileAttributes.Empty);
            _fifo.Enqueue8Pixels(Zip(0b10101011, 0b11100111, false), TileAttributes.Empty);
            Assert.That(_fifo.DequeuePixel(), Is.EqualTo(0b11));
            Assert.That(_fifo.DequeuePixel(), Is.EqualTo(0b11));
            Assert.That(_fifo.DequeuePixel(), Is.EqualTo(0b10));
            Assert.That(_fifo.DequeuePixel(), Is.EqualTo(0b10));
            Assert.That(_fifo.DequeuePixel(), Is.EqualTo(0b01));
        }

        [Test]
        public void TestZip()
        {
            Assert.That(Zip(0b11001001, 0b11110000, false), Is.EqualTo(new int[] { 3, 3, 2, 2, 1, 0, 0, 1 }));
            Assert.That(Zip(0b11001001, 0b11110000, true), Is.EqualTo(new int[] { 1, 0, 0, 1, 2, 2, 3, 3 }));
        }

        private static int[] Zip(int data1, int data2, bool reverse) => Fetcher.Zip(data1, data2, reverse, new int[8]);

        private static List<int> ArrayQueueAsList(IntQueue queue)
        {
            var l = new List<int>(queue.Size());
            for (var i = 0; i < queue.Size(); i++)
            {
                l.Add(queue.Get(i));
            }
            return l;
        }
    }
}