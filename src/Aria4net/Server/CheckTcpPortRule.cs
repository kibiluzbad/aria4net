using System.Linq;
using System.Net.NetworkInformation;

namespace Aria4net.Server
{
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
}