using System;

namespace Aria4net.Server
{
    public class Aria2cException : ApplicationException
    {
        public Aria2cException(int exitCode, string message)
            :base(string.Format("C�digo do erro {0}. {1}",exitCode,message))
        {
            
        }
    }
}