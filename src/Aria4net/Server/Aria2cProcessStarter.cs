using System;
using System.Diagnostics;
using Aria4net.Common;
using Aria4net.Exceptions;
using NLog;

namespace Aria4net.Server
{
    public class Aria2cProcessStarter : ProcessStarter
    {
        private readonly Aria2cConfig _config;
        public string DownloadedFilesDirPath { get; set; }
        
        public Aria2cProcessStarter(IFileFinder fileFinder, Aria2cConfig config, Logger logger) : base(fileFinder, logger)
        {
            _config = config;
        }

        public override bool IsRunning()
        {
            var pname = Process.GetProcessesByName("aria2c");
            return (0 < pname.Length);
        }

        protected override void ProcessOnExited(Process sender, EventArgs eventArgs)
        {
            try
            {
               Logger.Info("Processo executou com código {0}.", sender.ExitCode);
                if (0 <= sender.ExitCode)
                    throw new Aria2cException(sender.ExitCode, sender.StandardError.ReadToEnd());

            }
            catch (InvalidOperationException) // Ignore if process is not running
            { }
        }

        protected override void ConfigureProcess(Process process)
        {
            Logger.Info("Configurando processo.");
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        protected override string GetArguments()
        {
            Logger.Info("Definindo argumentos do processo.");

            return string.Format("--enable-rpc --dir=\"{0}\" --remove-control-file=true --auto-save-interval=0 --listen-port={1} --rpc-listen-port={2} --follow-torrent=false --file-allocation=trunc -V -c --show-console-readout=false --stop-with-process={3}", DownloadedFilesDirPath.Trim(), _config.Port, _config.RpcPort, System.Diagnostics.Process.GetCurrentProcess().Id);
        }
   }
}