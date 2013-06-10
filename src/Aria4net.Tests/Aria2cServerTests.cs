using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aria4net.Common;
using Aria4net.Server;
using Moq;
using NUnit.Framework;

namespace Aria4net.Tests
{
    [TestFixture]
    public class Aria2cServerTests
    {
        [Test]
        public void When_calling_Start_should_call_Run_on_IProcessStarter()
        {
            var mockProcessStarter = new Mock<IProcessStarter>();

            IServer server = new Aria2cServer(mockProcessStarter.Object);
            server.Start();

            mockProcessStarter.Verify(c=>c.Run(), Times.Once());
        }

        [Test]
        public void When_calling_Stop_should_call_Exit_on_IProcessStarter()
        {
            var mockProcessStarter = new Mock<IProcessStarter>();

            IServer server = new Aria2cServer(mockProcessStarter.Object);
            server.Stop();

            mockProcessStarter.Verify(c => c.Exit(), Times.Once());
        }
    }
}
