using System;

namespace Aria4net.Common
{
    public interface IProcessStarter : IDisposable
    {
        void Run();
        void Exit();
    }
}