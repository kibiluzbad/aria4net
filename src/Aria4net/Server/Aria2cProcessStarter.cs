using System;
using System.Diagnostics;
using Aria4net.Common;

namespace Aria4net.Server
{
    public class Aria2cProcessStarter : ProcessStarter
    {
        private readonly Aria2cConfig _config;
        public string DownloadedFilesDirPath { get; set; }
        
        public Aria2cProcessStarter(IFileFinder fileFinder, Aria2cConfig config) : base(fileFinder)
        {
            _config = config;
        }

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
            return string.Format("--enable-rpc --dir={0} -c --listen-port={1} --rpc-listen-port={2} {3}", DownloadedFilesDirPath.Trim(), _config.Port, _config.RpcPort, _config.OtherParameters);
        }
    }

    public class Aria2cException : ApplicationException
    {
        public Aria2cException(int exitCode, string message)
            :base(string.Format("C�digo do erro {0}. {1}",exitCode,message))
        {
            
        }
    }
}