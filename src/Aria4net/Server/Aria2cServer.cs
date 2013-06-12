using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Aria4net.Common;
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
        }

        public void Dispose()
        {
            if(null == _processStarter) return;

            _processStarter.Dispose();
        }
    }

    public interface IServerValidationRunner
    {
        void Run();
        void AddRule(IServerValidationRule getRuleForJsonRpcPort);
    }

    public class DefaultValidationRunner : IServerValidationRunner
    {
        private ICollection<IServerValidationRule> _rules;
        
        public DefaultValidationRunner()
        {
            _rules = new HashSet<IServerValidationRule>();
        }
        
        public void Run()
        {
            foreach (var rule in _rules)
            {
                rule.Execute();
            }
        }

        public void AddRule(IServerValidationRule rule)
        {
            _rules.Add(rule);
        }
    }

    public interface IServerValidationRule
    {
        void Execute();
    }

    public class CheckTcpPortRule : IServerValidationRule
    {
        public int Port { get; set; }

        public void Execute()
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            if (tcpConnInfoArray.Any(tcpi => tcpi.LocalEndPoint.Port == Port))
                throw new TcpPortNotAvailableExcpetion(Port);
        }
    }

    public class TcpPortNotAvailableExcpetion : Exception
    {
        public TcpPortNotAvailableExcpetion(int port, Exception ex = null)
            : base(string.Format("A porta tcp {0} não está disponivel para ser utilizada", port),ex)
        {
            
        }
    }
}