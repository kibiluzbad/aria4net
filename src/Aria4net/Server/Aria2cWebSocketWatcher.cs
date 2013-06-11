using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aria4net.Common;
using NLog;
using Newtonsoft.Json;
using WebSocket4Net;

namespace Aria4net.Server
{
    public class Aria2cWebSocketWatcher : IServerWatcher
    {
        private readonly Aria2cConfig _config;
        private readonly Logger _logger;
        private readonly WebSocket _socket;
        private readonly IDictionary<string, IDictionary<Guid,Action<string>>> _actions;

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
            _socket.Opened += (sender, args) => _logger.Info("Websocket connection opened.");
            
            _socket.Closed += (sender, args) => _logger.Info("Websocket connection closed.");

            _socket.MessageReceived += SocketOnMessageReceived;

            _socket.Error += (sender, args) => _logger.FatalException(args.Exception.Message,args.Exception);
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

            foreach (var action in Actions[message.Method])
            {
                _logger.Info("Invoking action for {0}.", message.Method);

                var aria2cParameter = message.Params.FirstOrDefault();
                if (aria2cParameter != null) action.Value(aria2cParameter.Gid);
            }
        }

        public virtual IServerWatcher Connect()
        {
            _socket.Open();
            return this;
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
    }
}