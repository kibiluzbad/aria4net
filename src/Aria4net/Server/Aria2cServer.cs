using System;
using Aria4net.Common;

namespace Aria4net.Server
{
    public class Aria2cServer : IServer, IDisposable
    {
        private readonly IProcessStarter _processStarter;

        public Aria2cServer(IProcessStarter processStarter)
        {
            _processStarter = processStarter;
        }

        public void Start()
        {
            _processStarter.Run();
        }

        public void Stop()
        {
            _processStarter.Exit();
        }

        public void Dispose()
        {
            if(null == _processStarter) return;

            _processStarter.Dispose();
        }
    }
}