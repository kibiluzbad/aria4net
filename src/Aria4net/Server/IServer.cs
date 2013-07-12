using System;
using SuperSocket.ClientEngine;

namespace Aria4net.Server
{
    public interface IServer
    {
        bool IsRunning { get; }
        void Start();
        void Stop();

        event EventHandler<EventArgs> Started;
        event EventHandler<EventArgs> Stoped;
        event EventHandler<ErrorEventArgs> OnError;
    }
}