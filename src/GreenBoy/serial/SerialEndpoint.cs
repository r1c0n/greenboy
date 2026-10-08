namespace GreenBoy.serial
{
    public interface SerialEndpoint
    {
        bool externalClockPulsed();

        /// <summary>
        /// Exchanges a complete byte when a transfer finishes and returns the received byte.
        /// </summary>
        int transfer(int outgoing);
    }

    public class NullSerialEndpoint : SerialEndpoint
    {
        public bool externalClockPulsed() => false;

        public int transfer(int outgoing)
        {
            return 0xff;
        }
    }
}
