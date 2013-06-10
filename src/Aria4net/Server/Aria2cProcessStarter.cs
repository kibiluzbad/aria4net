using System;
using System.Diagnostics;
using Aria4net.Common;

namespace Aria4net.Server
{
    public class Aria2cProcessStarter : ProcessStarter
    {
        public string DownloadedFilesDirPath { get; set; }
        
        public Aria2cProcessStarter(IFileFinder fileFinder) : base(fileFinder)
        { }

        protected override void ProcessOnExited(Process sender, EventArgs eventArgs)
        {
            if (0 < sender.ExitCode) 
                throw new Aria2cException(sender.ExitCode, sender.StandardError.ReadToEnd());
        }

        protected override void ConfigureProcess(Process process)
        {
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        protected override string GetArguments()
        {
            return string.Format("--enable-rpc --dir={0} -c ", DownloadedFilesDirPath);
        }
    }

    public class Aria2cException : ApplicationException
    {
        public Aria2cException(int exitCode, string message)
            :base(string.Format("Código do erro {0}. {1}",exitCode,message))
        {
            
        }
    }
}