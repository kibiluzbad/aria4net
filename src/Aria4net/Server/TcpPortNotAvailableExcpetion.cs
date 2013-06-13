using System;

namespace Aria4net.Server
{
    public class TcpPortNotAvailableExcpetion : Exception
    {
        public TcpPortNotAvailableExcpetion(int port, Exception ex = null)
            : base(string.Format("A porta tcp {0} não está disponivel para ser utilizada", port),ex)
        {
            
        }
    }
}