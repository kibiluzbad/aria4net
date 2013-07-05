using System;

namespace Aria4net.Exceptions
{
    public class Aria2cException : ApplicationException
    {
        public Aria2cException(int exitCode, string message, Exception inner = null)
            :base(string.Format("Código do erro {0}. {1}",exitCode,message), inner)
        {
            
        }
    }
}