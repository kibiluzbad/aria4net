using System.Diagnostics;
using Aria4net.Common;
using Aria4net.Server;
using NLog;

namespace Aria4net.IntegrationTests
{
    internal class Aria2cProcessStarterWithWindow : Aria2cProcessStarter
    {
        public Aria2cProcessStarterWithWindow(IFileFinder fileFinder, Aria2cConfig config, Logger logger) : base(fileFinder, config, logger)
        {
        }

        protected override void ConfigureProcess(System.Diagnostics.Process process)
        {
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        }
    }
}