using System;

namespace Aria4net.Server
{
    public interface IServer
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
    }
}