using GreenBoy.cpu;

namespace GreenBoy.Test.Integration.Support
{
    public interface ITracer
    {
        void Collect(Registers state);

        void Save();
    }
}