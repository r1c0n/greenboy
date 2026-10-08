using GreenBoy.gui;
using System.Threading;

namespace GreenBoy.gpu
{
    public delegate void FrameProducedEventHandler(object sender, byte[] frameData);

    public interface IDisplay : IRunnable
    {
        bool Enabled { get; set; }

        event FrameProducedEventHandler OnFrameProduced;

        void PutDmgPixel(int color);

        void PutColorPixel(int gbcRgb);

        void RequestRefresh();

        void WaitForRefresh();

        void WaitForRefresh(CancellationToken token) => WaitForRefresh();
    }
}
