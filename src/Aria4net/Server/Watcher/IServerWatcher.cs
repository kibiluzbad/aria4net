using System;
using System.Collections.Generic;

namespace Aria4net.Server.Watcher
{
    public interface IServerWatcher
    {
        Guid Subscribe(string method, Action<string> action);
        void Unsubscribe(Queue<Guid> keys);
        void Unsubscribe(string method);
        void Unsubscribe(string method, Guid key);
        IServerWatcher Connect();

        event EventHandler<EventArgs> ConnectionOpened;
        event EventHandler<EventArgs> ConnectionClosed;
        event EventHandler<EventArgs> OnError;
    }
}