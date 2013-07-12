using System;

namespace Aria4net.Exceptions
{
// ReSharper disable InconsistentNaming
    public class Aria2cException : ApplicationException
// ReSharper restore InconsistentNaming
    {
        public Aria2cException(int exitCode, string message, Exception inner = null)
            : base(string.Format("Código do erro {0}. {1}", exitCode, message), inner)
        {
        }
    }
}