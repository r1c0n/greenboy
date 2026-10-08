using System.Threading;

namespace GreenBoy.gui
{
    public interface IRunnable
    {
        void Run(CancellationToken token);
    }
}