using System;
using System.Diagnostics;
using NLog;

namespace Aria4net.Common
{
    public abstract class ProcessStarter : IProcessStarter
    {
        private readonly IFileFinder _fileFinder;
        protected readonly Logger Logger;
        private readonly Process _process;


        protected ProcessStarter(IFileFinder fileFinder, Logger logger)
        {
            _fileFinder = fileFinder;
            Logger = logger;
            _process = new Process();
        }

        public void Dispose()
        {
            if(null == _process) return;
            
            _process.Dispose();
        }

        public void Run()
        {
            
            _process.StartInfo.FileName = _fileFinder.Find();
            _process.StartInfo.Arguments = GetArguments();
            _process.EnableRaisingEvents = true;
            
            _process.Exited += (sender, args) => ProcessOnExited(sender as Process, args);
            
            ConfigureProcess(_process);

            Logger.Info("Iniciando processo {0}", _process.StartInfo.FileName);
            _process.Start();
        }

        public void Exit()
        {
            Logger.Info("Encerrando processo {0}", _process.StartInfo.FileName);
            if(IsRunning()) _process.Kill();
        }

        public abstract bool IsRunning();

        protected abstract void ProcessOnExited(Process sender, EventArgs eventArgs);

        protected abstract void ConfigureProcess(Process process);

        protected abstract string GetArguments();
    }
}