using GreenBoy.gpu;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using GreenBoy.gui;
using Image = System.Drawing.Image;

namespace GreenBoy.Windows
{
    public sealed class BitmapDisplayControl : Control, IDisplay
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;
        public static readonly float AspectRatio = DisplayWidth / (DisplayHeight * 1f);

        public static readonly int[] Colors = BitmapDisplay.Colors;

        private readonly BitmapDisplay _bitmapDisplay = new BitmapDisplay();
        private volatile byte[] _lastFrame;

        public BitmapDisplayControl()
        {
            _lastFrame = new GameboyDisplayFrame(new int[DisplayWidth * DisplayHeight]).ToBitmap();
            _bitmapDisplay.OnFrameProduced += FillAndDrawBuffer;
            SetStyle(ControlStyles.Opaque | ControlStyles.Selectable, false);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = System.Drawing.Color.FromArgb(Colors[0]);
            TabStop = false;
        }

        public event FrameProducedEventHandler OnFrameProduced;

        bool IDisplay.Enabled
        {
            get => DisplayEnabled;
            set => DisplayEnabled = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DisplayEnabled { get => _bitmapDisplay.Enabled; set => _bitmapDisplay.Enabled = value; }

        public void PutDmgPixel(int color)
        {
            _bitmapDisplay.PutDmgPixel(color);
        }

        public void PutColorPixel(int gbcRgb)
        {
            _bitmapDisplay.PutColorPixel(gbcRgb);
        }

        public static int TranslateGbcRgb(int gbcRgb)
        {
            return BitmapDisplay.TranslateGbcRgb(gbcRgb);
        }

        public void RequestRefresh()
        {
            _bitmapDisplay.RequestRefresh();
        }

        public void WaitForRefresh()
        {
            _bitmapDisplay.WaitForRefresh();
        }

        public void WaitForRefresh(CancellationToken token) => _bitmapDisplay.WaitForRefresh(token);

        public void Reset() => _bitmapDisplay.Reset();

        protected override void OnPaint(PaintEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new PaintEventHandler((_, __) => OnPaint(e)));
                return;
            }

            base.OnPaint(e);

            var width = ClientRectangle.Width;
            var height = ClientRectangle.Height;
            var aspectRatio = width * 1f / height;

            if (aspectRatio <= AspectRatio)
            {
                height = (int)Math.Floor(width / AspectRatio);
            }
            else
            {
                width = (int)Math.Floor(height * AspectRatio);
            }

            var x = 0;
            var y = 0;
            if (width < ClientRectangle.Width)
            {
                x += (ClientRectangle.Width - width) / 2;
            }
            else
            {
                y += (ClientRectangle.Height - height) / 2;
            }

            try
            {
                if (DisplayEnabled)
                {
                    using var imageStream = new MemoryStream(_lastFrame);
                    using var img = Image.FromStream(imageStream);

                    e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor; // Setting interpolation mode

                    if (e.Graphics.InterpolationMode == InterpolationMode.NearestNeighbor)
                    {
                        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    }

                    e.Graphics.DrawImage(img, x, y, width, height);
                }
                else
                {
                    using var brush = new SolidBrush(System.Drawing.Color.FromArgb(0xe6f8da));
                    e.Graphics.FillRectangle(brush, x, y, width, height);
                }
            }
            catch (ObjectDisposedException) { }
        }

        public void SaveLastFrame(string path)
        {
            File.WriteAllBytes(path, _lastFrame);
        }

        public void Run(CancellationToken token)
        {
            _bitmapDisplay.Run(token);
        }

        private void FillAndDrawBuffer(object sender, byte[] frame)
        {
            try
            {
                _lastFrame = frame;
                Invalidate();
            }
            catch (ObjectDisposedException) { }
            OnFrameProduced?.Invoke(this, frame);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int, int) ToRgb(int pixel)
        {
            var b = pixel & 255;
            var g = (pixel >> 8) & 255;
            var r = (pixel >> 16) & 255;
            return (r, g, b);
        }
    }
}
