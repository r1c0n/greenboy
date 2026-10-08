using GreenBoy.cpu;
using GreenBoy.serial;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace GreenBoy.Test.Unit.Serial
{
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class SerialPortTest
    {
        private const int DataAddress = 0xff01;
        private const int ControlAddress = 0xff02;
        private const int InterruptAddress = 0xff0f;
        private const int TicksPerTransfer = 4096;

        private InterruptManager _interrupts;
        private RecordingEndpoint _endpoint;
        private SerialPort _port;

        [SetUp]
        public void SetUp()
        {
            _interrupts = new InterruptManager(false);
            _interrupts.SetByte(InterruptAddress, 0);
            _endpoint = new RecordingEndpoint();
            _port = new SerialPort(_interrupts, _endpoint, new SpeedMode());
        }

        [Test]
        public void ExchangesOneByteAndRequestsInterruptAfterEightClockPulses()
        {
            _endpoint.Incoming = 0xa5;
            StartTransfer(0x42);

            Tick(TicksPerTransfer - 1);

            Assert.That(_endpoint.Outgoing, Is.Empty);
            Assert.That(_port.GetByte(ControlAddress) & 0x80, Is.EqualTo(0x80));
            Assert.That(_interrupts.GetByte(InterruptAddress) & 8, Is.Zero);

            _port.Tick();

            Assert.That(_endpoint.Outgoing, Is.EqualTo(new[] { 0x42 }));
            Assert.That(_port.GetByte(DataAddress), Is.EqualTo(0xa5));
            Assert.That(_port.GetByte(ControlAddress) & 0x80, Is.Zero);
            Assert.That(_interrupts.GetByte(InterruptAddress) & 8, Is.EqualTo(8));
        }

        [Test]
        public void ConsecutiveTransfersEachExchangeTheirOwnByteOnce()
        {
            StartTransfer('A');
            Tick(TicksPerTransfer);
            _interrupts.SetByte(InterruptAddress, 0);
            StartTransfer('B');
            Tick(TicksPerTransfer - 1);

            Assert.That(_endpoint.Outgoing, Is.EqualTo(new[] { (int)'A' }));
            Assert.That(_interrupts.GetByte(InterruptAddress) & 8, Is.Zero);

            _port.Tick();
            Tick(TicksPerTransfer);

            Assert.That(_endpoint.Outgoing, Is.EqualTo(new[] { (int)'A', (int)'B' }));
        }

        [Test]
        public void ClearingTransferFlagAbortsWithoutExchangingByteOrRequestingInterrupt()
        {
            StartTransfer(0x42);
            Tick(TicksPerTransfer / 2);
            _port.SetByte(ControlAddress, 0x01);
            Tick(TicksPerTransfer);

            Assert.That(_endpoint.Outgoing, Is.Empty);
            Assert.That(_interrupts.GetByte(InterruptAddress) & 8, Is.Zero);
        }

        [Test]
        public void SettingTransferFlagAgainRestartsTheClockCount()
        {
            StartTransfer(0x42);
            Tick(TicksPerTransfer / 2);
            _port.SetByte(ControlAddress, 0x81);
            Tick(TicksPerTransfer - 1);

            Assert.That(_endpoint.Outgoing, Is.Empty);

            _port.Tick();

            Assert.That(_endpoint.Outgoing, Is.EqualTo(new[] { 0x42 }));
        }

        [Test]
        public void ExternalClockTransferWaitsForClockPulses()
        {
            _port.SetByte(DataAddress, 0x42);
            _port.SetByte(ControlAddress, 0x80);
            Tick(TicksPerTransfer * 2);

            Assert.That(_endpoint.Outgoing, Is.Empty);
            Assert.That(_port.GetByte(ControlAddress) & 0x80, Is.EqualTo(0x80));
            Assert.That(_interrupts.GetByte(InterruptAddress) & 8, Is.Zero);

            _endpoint.ClockPulsed = true;
            Tick(TicksPerTransfer);

            Assert.That(_endpoint.Outgoing, Is.EqualTo(new[] { 0x42 }));
            Assert.That(_port.GetByte(ControlAddress) & 0x80, Is.Zero);
        }

        [Test]
        public void FailedExchangeCompletesWithDisconnectedInputAndRequestsInterrupt()
        {
            _endpoint.FailTransfer = true;
            StartTransfer(0x42);
            Tick(TicksPerTransfer);

            Assert.That(_endpoint.Outgoing, Is.EqualTo(new[] { 0x42 }));
            Assert.That(_port.GetByte(DataAddress), Is.EqualTo(0xff));
            Assert.That(_port.GetByte(ControlAddress) & 0x80, Is.Zero);
            Assert.That(_interrupts.GetByte(InterruptAddress) & 8, Is.EqualTo(8));
        }

        [Test]
        public void DisconnectedEndpointReceivesAllHighBits()
        {
            _port = new SerialPort(_interrupts, new NullSerialEndpoint(), new SpeedMode());
            StartTransfer(0x42);
            Tick(TicksPerTransfer);

            Assert.That(_port.GetByte(DataAddress), Is.EqualTo(0xff));
        }

        private void StartTransfer(int outgoing)
        {
            _port.SetByte(DataAddress, outgoing);
            _port.SetByte(ControlAddress, 0x81);
        }

        private void Tick(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _port.Tick();
            }
        }

        private sealed class RecordingEndpoint : SerialEndpoint
        {
            public List<int> Outgoing { get; } = new List<int>();
            public int Incoming { get; set; } = 0xff;
            public bool ClockPulsed { get; set; }
            public bool FailTransfer { get; set; }

            public bool externalClockPulsed() => ClockPulsed;

            public int transfer(int outgoing)
            {
                Outgoing.Add(outgoing);
                if (FailTransfer)
                {
                    throw new IOException("Serial endpoint disconnected.");
                }

                return Incoming;
            }
        }
    }
}
