using System;

namespace Aria4net.Exceptions
{
    public class TcpPortNotAvailableException : Exception
    {
        public TcpPortNotAvailableException(int port, Exception ex = null)
            : base(string.Format("A porta tcp {0} não está disponivel para ser utilizada", port), ex)
        {
        }
    }
}