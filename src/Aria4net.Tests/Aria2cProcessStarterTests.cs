using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Aria4net.Common;
using Aria4net.Server;
using Moq;
using NLog;
using NUnit.Framework;

namespace Aria4net.Tests
{
    [TestFixture]
    public class Aria2cProcessStarterTests
    {
        [Test]
        public void When_calling_Run_should_call_Find_in_IFileFinder()
        {
            var mockFileFinder = new Mock<IFileFinder>();
            var fakeLogger = new Mock<Logger>();
            var config = new Aria2cConfig {Port = "7000"};

            mockFileFinder
                .Setup(c => c.Find())
                .Returns(Environment.SystemDirectory + "\\notepad.exe");

            IProcessStarter processStarter = new Aria2cProcessStarter(mockFileFinder.Object, config, fakeLogger.Object)
                {
                    DownloadedFilesDirPath = () => Assembly.GetExecutingAssembly().Location
                };

            processStarter.Run();

            mockFileFinder.Verify(c=>c.Find(), Times.Once());
        }

        [Test]
        public void Can_exit_process()
        {
            var fakeFileFinder = new Mock<IFileFinder>();
            var fakeLogger = new Mock<Logger>();
            var config = new Aria2cConfig { Port = "7000" };

            fakeFileFinder
                .Setup(c => c.Find())
                .Returns(Environment.SystemDirectory + "\\notepad.exe");

            IProcessStarter processStarter = new Aria2cProcessStarter(fakeFileFinder.Object, config, fakeLogger.Object)
            {
                DownloadedFilesDirPath = () => Assembly.GetExecutingAssembly().Location
            };

            processStarter.Run();

            Thread.Sleep(1000);

            processStarter.Exit();
        }
    }
}
