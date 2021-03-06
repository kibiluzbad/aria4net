using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Aria4net.Common;
using Aria4net.Exceptions;
using NLog;
using Newtonsoft.Json;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

namespace Aria4net.Server.Watcher
{
// ReSharper disable InconsistentNaming
    public class Aria2cWebSocketWatcher : IServerWatcher, IDisposable
// ReSharper restore InconsistentNaming
    {
        private readonly Aria2cConfig _config;
        private readonly Logger _logger;
        private readonly WebSocket _socket;

        public Func<MessageReceivedEventArgs, Aria2cWebSocketMessage> MessageDeserializer =
            p =>
                {
                    var serializer = new JsonSerializer();
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

        public Aria2cWebSocketWatcher(Aria2cConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;
            _socket = new WebSocket(_config.WebSocketUrl);

            AttachEvents();
        }

        public void Dispose()
        {
            if (null == _socket) return;

            Disconnect();

            _socket.Opened -= OnSocketOnOpened;
            _socket.Closed -= OnSocketOnClosed;
            _socket.Error -= OnSocketOnError;
        }

        public event EventHandler<EventArgs> ConnectionOpened;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<ErrorEventArgs> OnError;

        public virtual IServerWatcher Connect()
        {
            _socket.Open();
            return this;
        }

        public IServerWatcher Disconnect()
        {
            if (_socket.State == WebSocketState.Open) _socket.Close();
            return this;
        }

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
                                         ISubject<Aria2cClientEventArgs> subject = new Subject<Aria2cClientEventArgs>();
                                         string gid = message.Params.FirstOrDefault().Gid;

                                         if (gid != keySelector()) return;

                                         switch (message.Method)
                                         {
                                             case "aria2.onDownloadStop":
                                                 subject.OnCompleted();
                                                 if (null != stoped) stoped(getData(gid));
                                                 if (null != token) token.Dispose();
                                                 break;
                                             case "aria2.onDownloadPause":
                                                 subject.OnCompleted();
                                                 if (null != token) token.Dispose();
                                                 if (null != paused) paused(getData(gid));
                                                 break;
                                             case "aria2.onDownloadError":
                                                 if (null != error) error(getData(gid));
                                                 break;
                                             case "aria2.onBtDownloadComplete":
                                             case "aria2.onDownloadComplete":
                                                 subject.OnCompleted();
                                                 if (null != token) token.Dispose();
                                                 if (null != completed)
                                                     try
                                                     {
                                                         completed(getData(gid));
                                                     }
                                                     catch (Aria2cException aex)
                                                     {
                                                         _logger.DebugException(aex.Message, aex);
                                                     }

                                                 break;
                                             case "aria2.onDownloadStart":
                                                 Aria2cClientEventArgs args = getData(gid);

                                                 if (null != started) started(args);
                                                 
                                                 subject.OnNext(args);

                                                 if(null != progress)subject.Subscribe(progress);

                                                 if (args.Status.Completed)
                                                 {
                                                     if (null != completed) completed(args);
                                                     return;
                                                 }

                                                 if (null == getProgress) return;

                                                 token = StartReportingProgress(args, getProgress,completed, subject);
                                                 break;
                                         }
                                     },
                                 ex => { if (null != OnError) OnError(this, new ErrorEventArgs(ex)); }
                                 ,
                                 () => _logger.Info("Observable liberado."));
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
            _logger.DebugException(args.Exception.Message, args.Exception);
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

        protected virtual IDisposable StartReportingProgress(Aria2cClientEventArgs args, Func<Aria2cClientEventArgs, Aria2cClientEventArgs> getProgress, Action<Aria2cClientEventArgs> completed, ISubject<Aria2cClientEventArgs> subject)
        {
            _logger.Info("Observando progresso de {0}.", args.Status.Gid);

            ThreadPoolScheduler scheduler = Scheduler.ThreadPool;

            Action<Action> work = self =>
                {
                    try
                    {
                        Aria2cClientEventArgs eventArgs = getProgress(args);

                        if (eventArgs.Status.Completed)
                        {
                            completed(eventArgs);
                            return;
                        }
                        subject.OnNext(eventArgs);
                    }
                    catch (Aria2cException aex)
                    {
                        _logger.DebugException(aex.Message, aex);
                        subject.OnError(aex);
                        return;
                    }

                    Thread.Sleep(1000);
                    self();
                };

            return scheduler.Schedule(work);
        }
    }
}