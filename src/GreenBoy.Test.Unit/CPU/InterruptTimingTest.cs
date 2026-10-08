using GreenBoy.cpu;
using GreenBoy.gpu;
using GreenBoy.memory;
using NUnit.Framework;

namespace GreenBoy.Test.Unit.CPU
{
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class InterruptTimingTest
    {
        private const int ProgramAddress = 0x100;
        private InterruptManager _interrupts;
        private Ram _ram;
        private Cpu _cpu;

        [SetUp]
        public void SetUp()
        {
            _interrupts = new InterruptManager(false);
            _interrupts.SetByte(0xff0f, 0);
            _interrupts.SetByte(0xffff, 1);
            _ram = new Ram(0, 0x10000);
            var memory = new Mmu();
            memory.AddAddressSpace(_interrupts);
            memory.AddAddressSpace(_ram);
            _cpu = new Cpu(memory, _interrupts, null, new NullDisplay(), new SpeedMode());
            _cpu.Registers.PC = ProgramAddress;
            _cpu.Registers.SP = 0xfffe;
        }

        [Test]
        public void EiTakesEffectWhenHaltFinishesAndServicesTheNextInterrupt()
        {
            LoadProgram(0xfb, 0x76, 0xf3); // EI; HALT; DI
            Tick(4);
            Assert.That(_interrupts.IsIme(), Is.False);

            Tick(4);

            Assert.That(_interrupts.IsIme(), Is.True);
            Assert.That(_cpu.State, Is.EqualTo(State.HALTED));
            Tick(12);
            Assert.That(_cpu.Registers.PC, Is.EqualTo(ProgramAddress + 2));

            _interrupts.RequestInterrupt(InterruptManager.InterruptType.VBlank);
            Tick(20);

            Assert.That(_cpu.State, Is.EqualTo(State.OPCODE));
            Assert.That(_cpu.Registers.PC, Is.EqualTo(0x40));
            Assert.That(_cpu.Registers.SP, Is.EqualTo(0xfffc));
            Assert.That(_ram.GetByte(0xfffc), Is.EqualTo(0x02));
            Assert.That(_ram.GetByte(0xfffd), Is.EqualTo(0x01));
            Assert.That(_interrupts.IsIme(), Is.False);
            Assert.That(_interrupts.GetByte(0xff0f) & 1, Is.Zero);
        }

        [Test]
        public void EiBeforeHaltWithAPendingInterruptReturnsToHaltAfterTheHandler()
        {
            LoadProgram(0xfb, 0x76, 0x04); // EI; HALT; INC B
            _ram.SetByte(0x40, 0xd9); // RETI
            _interrupts.RequestInterrupt(InterruptManager.InterruptType.VBlank);
            Tick(8);
            Tick(20);

            Assert.That(_cpu.Registers.PC, Is.EqualTo(0x40));
            Assert.That(_ram.GetByte(0xfffc), Is.EqualTo(0x01));
            Assert.That(_ram.GetByte(0xfffd), Is.EqualTo(0x01));

            Tick(16);

            Assert.That(_cpu.Registers.PC, Is.EqualTo(ProgramAddress + 1));
            Assert.That(_cpu.Registers.SP, Is.EqualTo(0xfffe));
            Assert.That(_interrupts.IsIme(), Is.True);

            Tick(16);

            Assert.That(_cpu.State, Is.EqualTo(State.HALTED));
            Assert.That(_cpu.Registers.B, Is.Zero);
            Assert.That(_cpu.Registers.PC, Is.EqualTo(ProgramAddress + 2));
        }

        [Test]
        public void DiCancelsTheDelayedEnableBeforeTheNextInstruction()
        {
            LoadProgram(0xfb, 0xf3, 0x04); // EI; DI; INC B
            Tick(8);
            _interrupts.RequestInterrupt(InterruptManager.InterruptType.VBlank);
            Tick(4);

            Assert.That(_interrupts.IsIme(), Is.False);
            Assert.That(_cpu.Registers.B, Is.EqualTo(1));
            Assert.That(_cpu.Registers.PC, Is.EqualTo(ProgramAddress + 3));
            Assert.That(_interrupts.GetByte(0xff0f) & 1, Is.EqualTo(1));
        }

        [Test]
        public void HaltWithImeDisabledResumesWithoutAnExtraCycleOrInterruptService()
        {
            LoadProgram(0x76, 0x04); // HALT; INC B
            Tick(16);
            Assert.That(_cpu.State, Is.EqualTo(State.HALTED));
            Assert.That(_cpu.Registers.B, Is.Zero);

            _interrupts.RequestInterrupt(InterruptManager.InterruptType.VBlank);
            Tick(4);

            Assert.That(_cpu.Registers.B, Is.EqualTo(1));
            Assert.That(_cpu.Registers.PC, Is.EqualTo(ProgramAddress + 2));
            Assert.That(_cpu.Registers.SP, Is.EqualTo(0xfffe));
            Assert.That(_interrupts.GetByte(0xff0f) & 1, Is.EqualTo(1));
        }

        [Test]
        public void PendingInterruptWithImeDisabledCausesTheHaltBug()
        {
            LoadProgram(0x76, 0x3e, 0x12); // HALT; LD A,$12
            _interrupts.RequestInterrupt(InterruptManager.InterruptType.VBlank);
            Tick(12);

            Assert.That(_cpu.Registers.A, Is.EqualTo(0x3e));
            Assert.That(_cpu.Registers.PC, Is.EqualTo(ProgramAddress + 2));
            Assert.That(_cpu.Registers.SP, Is.EqualTo(0xfffe));
        }

        [Test]
        public void UnusedInterruptBitsDoNotWakeHalt()
        {
            LoadProgram(0x76, 0x04); // HALT; INC B
            _interrupts.SetByte(0xffff, 0xe0);
            Tick(16);

            Assert.That(_interrupts.IsInterruptRequested(), Is.False);
            Assert.That(_cpu.State, Is.EqualTo(State.HALTED));
            Assert.That(_cpu.Registers.B, Is.Zero);
            Assert.That(_cpu.Registers.PC, Is.EqualTo(ProgramAddress + 1));
        }

        private void LoadProgram(params int[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                _ram.SetByte(ProgramAddress + i, bytes[i]);
            }
        }

        private void Tick(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _cpu.Tick();
            }
        }
    }
}
