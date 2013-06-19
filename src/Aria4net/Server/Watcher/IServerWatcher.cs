using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Aria4net.Common;
using SuperSocket.ClientEngine;

namespace Aria4net.Server.Watcher
{
    public interface IServerWatcher
    {
        IDisposable Subscribe(Func<string> keySelector,
                              Func<string, Aria2cClientEventArgs> getData,
                              Func<Aria2cClientEventArgs, Aria2cClientEventArgs> getProgress = null,
                              Action<Aria2cClientEventArgs> started = null,
                              Action<Aria2cClientEventArgs> progress = null,
                              Action<Aria2cClientEventArgs> completed = null,
                              Action<Aria2cClientEventArgs> error = null,
                              Action<Aria2cClientEventArgs> stoped = null,
                              Action<Aria2cClientEventArgs> paused = null);
        
        IServerWatcher Connect();
        IServerWatcher Disconnect();

        event EventHandler<EventArgs> ConnectionOpened;
        event EventHandler<EventArgs> ConnectionClosed;
        event EventHandler<ErrorEventArgs> OnError;
    }
}