using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Aria4net.Client;
using Aria4net.Common;
using Aria4net.Exceptions;
using NLog;
using Newtonsoft.Json;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

namespace Aria4net.Server.Watcher
{
    public class Aria2cWebSocketWatcher : IServerWatcher, IDisposable
    {
        private readonly Aria2cConfig _config;
        private readonly Logger _logger;
        private readonly WebSocket _socket;

        public event EventHandler<EventArgs> ConnectionOpened;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<ErrorEventArgs> OnError;

        public Aria2cWebSocketWatcher(Aria2cConfig config, NLog.Logger logger)
        {
            _config = config;
            _logger = logger;
            _socket = new WebSocket(_config.WebSocketUrl);

            AttachEvents();
        }

        private void AttachEvents()
        {
            _socket.Opened += OnSocketOnOpened;

            _socket.Closed += OnSocketOnClosed;

            _socket.Error += OnSocketOnError;
        }

        private void OnSocketOnError(object sender, ErrorEventArgs args)
        {
            if (null != OnError) OnError(this, args);
            _logger.FatalException(args.Exception.Message, args.Exception);
        }

        private void OnSocketOnClosed(object sender, EventArgs args)
        {
            if (null != ConnectionClosed) ConnectionClosed(this, args);
            _logger.Info("Websocket connection closed.");
        }

        private void OnSocketOnOpened(object sender, EventArgs args)
        {
            if (null != ConnectionOpened) ConnectionOpened(this, args);
            _logger.Info("Websocket connection opened.");
        }

        public virtual IServerWatcher Connect()
        {
            _socket.Open();
            return this;
        }

        public IServerWatcher Disconnect()
        {
            if(_socket.State == WebSocketState.Open) _socket.Close();
            return this;
        }

        public Func<MessageReceivedEventArgs, Aria2cWebSocketMessage> MessageDeserializer =
        p=>
    {
        var serializer = new Newtonsoft.Json.JsonSerializer();
        using (
            var reader =
                new JsonTextReader(
                    new StringReader(
                        p.Message)))
        {
            var message =
                serializer
                    .Deserialize<Aria2cWebSocketMessage>
                    (reader);
            return message;
        }
    };


        public virtual IDisposable Subscribe(Func<string> keySelector,
                                             Func<string, Aria2cClientEventArgs> getData,
                                             Func<Aria2cClientEventArgs, Aria2cClientEventArgs> getProgress = null,
                                             Action<Aria2cClientEventArgs> started = null,
                                             Action<Aria2cClientEventArgs> progress = null,
                                             Action<Aria2cClientEventArgs> completed = null,
                                             Action<Aria2cClientEventArgs> error = null,
                                             Action<Aria2cClientEventArgs> stoped = null,
                                             Action<Aria2cClientEventArgs> paused = null)
        {
            IDisposable token = null;

            return Observable.FromEventPattern<MessageReceivedEventArgs>(handler => _socket.MessageReceived += handler,
                                                                         handler => _socket.MessageReceived -= handler)
                             .Select(c => MessageDeserializer(c.EventArgs))
                             .Where(c => null != c.Params)
                             .Subscribe(
                                 message =>
                                     {
                                         var gid = message.Params.FirstOrDefault().Gid;
                                         
                                         if (gid != keySelector()) return;

                                         switch (message.Method)
                                         {
                                             case "aria2.onDownloadStop":
                                                 if (null != stoped) stoped(getData(gid));
                                                 break;
                                             case "aria2.onDownloadPause":
                                                 if (null != paused) paused(getData(gid));
                                                 break;
                                             case "aria2.onDownloadError":
                                                 if (null != error) error(getData(gid));
                                                 break;
                                             case "aria2.onBtDownloadComplete":
                                             case "aria2.onDownloadComplete":
                                                 if (null != token) token.Dispose();
                                                 if (null != completed)
                                                     try
                                                     {
                                                         completed(getData(gid));
                                                     }
                                                     catch (Aria2cException aex)
                                                     {
                                                         _logger.ErrorException(aex.Message,aex);
                                                     }

                                                 break;
                                             case "aria2.onDownloadStart":
                                                 var args = getData(gid);
                                                 
                                                 if (null != started) started(args);
                                                 
                                                 if (args.Status.Completed)
                                                 {
                                                     if (null != completed) completed(args);
                                                     return;
                                                 }

                                                 if (null == getProgress) return;

                                                 token = StartReportingProgress(args, getProgress, progress, completed);
                                                 break;
                                         }
                                     },
                                 ex =>
                                     {
                                         if (null != OnError) OnError(this, new ErrorEventArgs(ex));
                                     }
                                 ,
                                 () => _logger.Info("Observable liberado."));
        }

        protected virtual IDisposable StartReportingProgress(Aria2cClientEventArgs args,
            Func<Aria2cClientEventArgs, Aria2cClientEventArgs> getProgress,
            Action<Aria2cClientEventArgs> progress,
            Action<Aria2cClientEventArgs> completed)
        {
            _logger.Info("Observando progresso de {0}.", args.Status.Gid);

            var scheduler = Scheduler.ThreadPool;
            IDisposable token = null;

            Action<Action> work = self =>
                {
                    try
                    {
                        Aria2cClientEventArgs eventArgs = getProgress(args);

                        if (eventArgs.Status.Completed)
                        {
                            completed(eventArgs);
                            token.Dispose();
                            return;
                        }

                        progress(eventArgs);
                    }
                    catch (Aria2cException aex)
                    {
                        _logger.FatalException(aex.Message,aex);
                        token.Dispose();
                        return;
                    }

                    Thread.Sleep(1000);
                    self();
                };

            return token = scheduler.Schedule(work);
        }

        public void Dispose()
        {
            if (null == _socket) return;

            Disconnect();

            _socket.Opened -= OnSocketOnOpened;
            _socket.Closed -= OnSocketOnClosed;
            _socket.Error -= OnSocketOnError;
        }
    }

    
}