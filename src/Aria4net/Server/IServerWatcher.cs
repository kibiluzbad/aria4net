using System;

namespace Aria4net.Server
{
    public interface IServerWatcher
    {
        void Subscribe(string method, Action<string> action);
        IServerWatcher Connect();
    }
}