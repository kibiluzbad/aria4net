using System;
using Aria4net.Common;
using Aria4net.Server.Validation;
using NLog;

namespace Aria4net.Server
{
    public class Aria2cServer : IServer, IDisposable
    {
        private readonly IProcessStarter _processStarter;
        private readonly IServerValidationRunner _serverValidationRunner;
        private readonly Aria2cConfig _config;
        private readonly Logger _logger;

        public Aria2cServer(IProcessStarter processStarter, 
            IServerValidationRunner serverValidationRunner,
            Aria2cConfig config,
            Logger logger)
        {
            _processStarter = processStarter;
            _serverValidationRunner = serverValidationRunner;
            _config = config;
            _logger = logger;
        }

        public void Start()
        {
            _logger.Info("Iniciando servidor");

            AddStartValidationRules();
            
            _serverValidationRunner.Run();

            _processStarter.Run();

            IsRunning = true;
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
        }

        public bool IsRunning { get; private set; }

        public void Dispose()
        {
            if(null == _processStarter) return;

            _processStarter.Dispose();
        }
    }
}