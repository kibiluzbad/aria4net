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
        private readonly IDictionary<string, IDictionary<Guid, Action<string>>> _actions;

        private volatile object _sync = new object();

        public IDictionary<string, IDictionary<Guid, Action<string>>> Actions
        {
            get { return _actions; }
        }

        public Aria2cWebSocketWatcher(Aria2cConfig config, NLog.Logger logger)
        {
            _config = config;
            _logger = logger;
            _socket = new WebSocket(_config.WebSocketUrl);
            _actions = new Dictionary<string, IDictionary<Guid, Action<string>>>();

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

        protected virtual void SocketOnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            _logger.Info("Websocket message recieved.");

            var serializer = new Newtonsoft.Json.JsonSerializer();

            using (var reader = new JsonTextReader(new StringReader(args.Message)))
            {
                var message = serializer.Deserialize<Aria2cWebSocketMessage>(reader);

                InvokeActions(message);
            }
        }

        protected virtual void InvokeActions(Aria2cWebSocketMessage message)
        {
            if (string.IsNullOrEmpty(("" + message.Method).Trim())) return;

            _logger.Info("Invoking actions for {0}.", message.Method);

            if (!Actions.ContainsKey(message.Method)) return;

            foreach (var action in Actions[message.Method].ToList())
            {
                _logger.Info("Invoking action {1} for {0}.", message.Method, action.Key);

                var aria2cParameter = message.Params.FirstOrDefault();
                if (aria2cParameter != null) action.Value(aria2cParameter.Gid);
            }
        }

        public virtual IServerWatcher Connect()
        {
            _socket.Open();
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
                                                 if (null != completed) completed(getData(gid));
                                                 if (null != token) token.Dispose();
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

                                                 token = StartReportingProgress(args, getProgress, progress);

                                                 if (getProgress(args).Status.Completed)
                                                 {
                                                     token.Dispose();
                                                     if (null != completed) completed(args);
                                                 }
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

        protected virtual IDisposable StartReportingProgress(Aria2cClientEventArgs args, Func<Aria2cClientEventArgs, Aria2cClientEventArgs> getProgress, Action<Aria2cClientEventArgs> progress)
        {
            _logger.Info("Observando progresso de {0}.", args.Status.Gid);

            var scheduler = Scheduler.ThreadPool;

            Action<Action> work = self =>
                {
                    Aria2cClientEventArgs eventArgs = getProgress(args);

                    if (eventArgs.Status.Completed)
                    {   return;
                    }

                    progress(eventArgs);
                    Thread.Sleep(500);
                    self();
                };

            return scheduler.Schedule(work);
        }

        public virtual Guid Subscribe(string method, Action<string> action)
        {
            lock (_sync)
            {
                var key = Guid.NewGuid();

                if (Actions.ContainsKey(method))
                {
                    Actions[method].Add(key,action);
                }
                else
                {
                    Actions[method] = new Dictionary<Guid, Action<string>>
                        {
                            {key,action}
                        };
                }
                return key;
            }
        }

        public virtual void Unsubscribe(string method, Guid key)
        {
            lock (_sync)
            {
                if (Actions.ContainsKey(method) &&
                    Actions[method].ContainsKey(key))
                {
                    Actions[method].Remove(key);
                }
            }
        }

        public virtual void Unsubscribe(string method)
        {
            lock (_sync)
            {
                if (Actions.ContainsKey(method))
                {
                    Actions.Remove(method);
                }
            }
        }

        public virtual void Unsubscribe(Queue<Guid> keys)
        {
            lock (_sync)
            {
                while (0 < keys.Count)
                {
                    var key = keys.Dequeue();

                    foreach (var action in Actions.Where(c => c.Value.ContainsKey(key)))
                    {
                        action.Value.Remove(key);
                    }
                }
            }
        }

        public event EventHandler<EventArgs> ConnectionOpened;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<ErrorEventArgs> OnError;

        public void Dispose()
        {
            if (null == _socket) return;

            _socket.Close();

            _socket.Opened -= OnSocketOnOpened;
            _socket.Closed -= OnSocketOnClosed;
            _socket.Error -= OnSocketOnError;
        }
    }

    
}