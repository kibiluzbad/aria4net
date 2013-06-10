using System;
using System.Diagnostics;

namespace Aria4net.Common
{
    public abstract class ProcessStarter : IProcessStarter
    {
        private readonly IFileFinder _fileFinder;
        private readonly Process _process;


        protected ProcessStarter(IFileFinder fileFinder)
        {
            _fileFinder = fileFinder;
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

            _process.Start();
        }

        public void Exit()
        {
            _process.Kill();
        }

        protected abstract void ProcessOnExited(Process sender, EventArgs eventArgs);

        protected abstract void ConfigureProcess(Process process);

        protected abstract string GetArguments();
    }
}