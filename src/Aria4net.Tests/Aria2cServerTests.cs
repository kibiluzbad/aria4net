using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aria4net.Common;
using Aria4net.Server;
using Moq;
using NLog;
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
            var fakeLogger = new Mock<Logger>();
            var fakeRunner = new Mock<IServerValidationRunner>();

            IServer server = new Aria2cServer(mockProcessStarter.Object,
                fakeRunner.Object,
                new Aria2cConfig(),
                fakeLogger.Object);
            server.Start();

            mockProcessStarter.Verify(c=>c.Run(), Times.Once());
        }

        [Test]
        public void When_calling_Stop_should_call_Exit_on_IProcessStarter()
        {
            var mockProcessStarter = new Mock<IProcessStarter>();
            var fakeLogger = new Mock<Logger>();
            var fakeRunner = new Mock<IServerValidationRunner>();

            IServer server = new Aria2cServer(mockProcessStarter.Object,
                fakeRunner.Object,
                new Aria2cConfig(),
                fakeLogger.Object);
            server.Stop();

            mockProcessStarter.Verify(c => c.Exit(), Times.Once());
        }
    }
}
