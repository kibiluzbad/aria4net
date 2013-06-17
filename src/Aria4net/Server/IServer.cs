using System;
using SuperSocket.ClientEngine;

namespace Aria4net.Server
{
    public interface IServer
    {
        void Start();
        void Stop();
        bool IsRunning { get; }

        event EventHandler<EventArgs> Started;
        event EventHandler<EventArgs> Stoped;
        event EventHandler<ErrorEventArgs> OnError;
    }
}