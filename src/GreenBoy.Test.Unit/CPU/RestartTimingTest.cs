using GreenBoy.cpu;
using GreenBoy.gpu;
using GreenBoy.memory;
using NUnit.Framework;
using System.Collections.Generic;

namespace GreenBoy.Test.Unit.CPU
{
    [TestFixture]
    public class RestartTimingTest
    {
        [TestCase(0xc7, 0x00)]
        [TestCase(0xcf, 0x08)]
        [TestCase(0xd7, 0x10)]
        [TestCase(0xdf, 0x18)]
        [TestCase(0xe7, 0x20)]
        [TestCase(0xef, 0x28)]
        [TestCase(0xf7, 0x30)]
        [TestCase(0xff, 0x38)]
        public void RestartWaitsBeforePushingTheReturnAddress(int opcode, int vector)
        {
            var memory = new RecordingMemory();
            memory.SetByte(0x1234, opcode);
            memory.Writes.Clear();
            var cpu = new Cpu(memory, new InterruptManager(false), null, new NullDisplay(), new SpeedMode());
            cpu.Registers.PC = 0x1234;
            cpu.Registers.SP = 0xfffe;

            Tick(cpu, memory, 8);

            Assert.That(memory.Writes, Is.Empty, "Opcode fetch and the internal cycle must precede stack writes.");
            Assert.That(cpu.Registers.SP, Is.EqualTo(0xfffe));

            Tick(cpu, memory, 4);

            Assert.That(memory.Writes, Is.EqualTo(new[] { (12, 0xfffd, 0x12) }));
            Assert.That(cpu.Registers.SP, Is.EqualTo(0xfffd));

            Tick(cpu, memory, 4);

            Assert.That(memory.Writes, Is.EqualTo(new[] { (12, 0xfffd, 0x12), (16, 0xfffc, 0x35) }));
            Assert.That(cpu.Registers.SP, Is.EqualTo(0xfffc));
            Assert.That(cpu.Registers.PC, Is.EqualTo(vector));
            Assert.That(cpu.State, Is.EqualTo(State.OPCODE));
        }

        private static void Tick(Cpu cpu, RecordingMemory memory, int count)
        {
            for (int i = 0; i < count; i++)
            {
                memory.Ticks++;
                cpu.Tick();
            }
        }

        private sealed class RecordingMemory : IAddressSpace
        {
            private readonly Ram _ram = new Ram(0, 0x10000);
            public int Ticks { get; set; }
            public List<(int Tick, int Address, int Value)> Writes { get; } = new List<(int, int, int)>();

            public bool Accepts(int address) => _ram.Accepts(address);
            public int GetByte(int address) => _ram.GetByte(address);

            public void SetByte(int address, int value)
            {
                Writes.Add((Ticks, address, value));
                _ram.SetByte(address, value);
            }
        }
    }
}
