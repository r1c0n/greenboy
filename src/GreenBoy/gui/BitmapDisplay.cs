using GreenBoy.gpu;
using System;
using System.Threading;

namespace GreenBoy.gui
{
    public class BitmapDisplay : IDisplay, IRunnable
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;

        public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };

        private readonly int[] _rgb;
        public bool Enabled { get; set; }
        private bool _doRefresh;
        private int _i;

        public event FrameProducedEventHandler OnFrameProduced;

        private readonly object _lockObject = new object();

        public BitmapDisplay()
        {
            _rgb = new int[DisplayWidth * DisplayHeight];
        }

        public void PutDmgPixel(int color)
        {
            _rgb[_i++] = Colors[color];
            _i = _i % _rgb.Length;
        }

        public void PutColorPixel(int gbcRgb)
        {
            _rgb[_i++] = TranslateGbcRgb(gbcRgb);
            _i %= _rgb.Length;
        }

        public static int TranslateGbcRgb(int gbcRgb)
        {
            var r = (gbcRgb >> 0) & 0x1f;
            var g = (gbcRgb >> 5) & 0x1f;
            var b = (gbcRgb >> 10) & 0x1f;
            var result = (r * 8) << 16;
            result |= (g * 8) << 8;
            result |= (b * 8) << 0;
            return result;
        }

        public void RequestRefresh() => SetRefreshFlag(true);

        public void Reset()
        {
            lock (_lockObject)
            {
                Array.Clear(_rgb, 0, _rgb.Length);
                _i = 0;
                _doRefresh = false;
                Enabled = false;
                Monitor.PulseAll(_lockObject);
            }
        }

        public void WaitForRefresh() => WaitForRefresh(CancellationToken.None);

        public void WaitForRefresh(CancellationToken token)
        {
            using var registration = token.Register(PulseWaiters);
            lock (_lockObject)
            {
                while (_doRefresh && !token.IsCancellationRequested)
                    Monitor.Wait(_lockObject);
            }
        }

        public void Run(CancellationToken token)
        {
            using var registration = token.Register(PulseWaiters);
            Enabled = true;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    lock (_lockObject)
                    {
                        while (!_doRefresh && !token.IsCancellationRequested)
                            Monitor.Wait(_lockObject);
                    }
                    if (token.IsCancellationRequested) break;

                    RefreshScreen();
                    SetRefreshFlag(false);
                }
            }
            finally
            {
                Enabled = false;
                SetRefreshFlag(false);
            }
        }

        private void RefreshScreen()
        {
            var frame = new GameboyDisplayFrame(_rgb);
            var bytes = frame.ToBitmap();

            OnFrameProduced?.Invoke(this, bytes);

            _i = 0;
        }

        private void SetRefreshFlag(bool flag)
        {
            lock (_lockObject)
            {
                _doRefresh = flag;
                Monitor.PulseAll(_lockObject);
            }
        }

        private void PulseWaiters()
        {
            lock (_lockObject) Monitor.PulseAll(_lockObject);
        }
    }
}
