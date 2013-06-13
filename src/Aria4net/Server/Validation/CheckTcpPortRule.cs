using System.Linq;
using System.Net.NetworkInformation;
using Aria4net.Exceptions;

namespace Aria4net.Server.Validation
{
    public class CheckTcpPortRule : IServerValidationRule
    {
        public int Port { get; set; }

        public void Execute()
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            if (tcpConnInfoArray.Any(tcpi => tcpi.LocalEndPoint.Port == Port && (tcpi.State == TcpState.Listen || tcpi.State == TcpState.Established)))
                throw new TcpPortNotAvailableException(Port);
        }
    }
}