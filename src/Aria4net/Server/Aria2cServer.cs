using System;
using System.Threading;
using Aria4net.Common;
using Aria4net.Server.Validation;
using Aria4net.Server.Watcher;
using NLog;
using SuperSocket.ClientEngine;

namespace Aria4net.Server
{
    public class Aria2cServer : IServer, IDisposable
    {
        private readonly IProcessStarter _processStarter;
        private readonly IServerValidationRunner _serverValidationRunner;
        private readonly Aria2cConfig _config;
        private readonly Logger _logger;
        private readonly IServerWatcher _serverWatcher;

        public Aria2cServer(IProcessStarter processStarter, 
            IServerValidationRunner serverValidationRunner,
            Aria2cConfig config,
            Logger logger,
            IServerWatcher serverWatcher)
        {
            _processStarter = processStarter;
            _serverValidationRunner = serverValidationRunner;
            _config = config;
            _logger = logger;
            _serverWatcher = serverWatcher;
        }

        public void Start()
        {
            _logger.Info("Iniciando servidor");

            AddStartValidationRules();

            _serverValidationRunner.Run();

            _processStarter.Run();

            var reset = new ManualResetEvent(false);

            _serverWatcher.ConnectionOpened += (sender, args) =>
                {
                    IsRunning = true;
                    if (null != Started) Started(this, new EventArgs());
                    reset.Set();
                };

            _serverWatcher.OnError += (sender, args) =>
                {
                    if (null != OnError) OnError(this, args);
                    reset.Set();
                };

            

            _serverWatcher.Connect();

            if (!reset.WaitOne(new TimeSpan(0, 5, 0)))
            {
                throw new TimeoutException("Não foi possivel abrir uma conexão com sevidor.");
            }
        }

        protected virtual void AddStartValidationRules()
        {
            _logger.Info("Executando regras de validação da inicialização do servidor");
            _serverValidationRunner.AddRule(GetRuleForJsonRpcPort());
            _serverValidationRunner.AddRule(GetRuleForBittorrentPort());
        }

        protected virtual IServerValidationRule GetRuleForBittorrentPort()
        {
            return new CheckTcpPortRule { Port = _config.Port };
        }

        protected virtual IServerValidationRule GetRuleForJsonRpcPort()
        {
            return new CheckTcpPortRule {Port = _config.RpcPort};
        }


        public void Stop()
        {
            _logger.Info("Parando servidor");
            _processStarter.Exit();
            IsRunning = false;
            if (null != Stoped) Stoped(this, new EventArgs());
        }

        public bool IsRunning { get; private set; }

        public event EventHandler<EventArgs> Started;
        public event EventHandler<EventArgs> Stoped;
        public event EventHandler<ErrorEventArgs> OnError;

        public void Dispose()
        {
            if(null == _processStarter) return;

            _processStarter.Dispose();
        }
    }
}