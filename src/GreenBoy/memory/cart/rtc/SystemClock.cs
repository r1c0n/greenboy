using System;

namespace GreenBoy.memory.cart.rtc
{
    public class SystemClock : IClock
    {
        public long CurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}