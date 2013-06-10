using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Aria4net.Common;
using Aria4net.Server;
using Moq;
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

            mockFileFinder
                .Setup(c => c.Find())
                .Returns(Environment.SystemDirectory + "\\notepad.exe");

            IProcessStarter processStarter = new Aria2cProcessStarter(mockFileFinder.Object)
                {
                    DownloadedFilesDirPath = Assembly.GetExecutingAssembly().Location
                };

            processStarter.Run();

            mockFileFinder.Verify(c=>c.Find(), Times.Once());
        }

        [Test]
        public void Can_exit_process()
        {
            var fakeFileFinder = new Mock<IFileFinder>();

            fakeFileFinder
                .Setup(c => c.Find())
                .Returns(Environment.SystemDirectory + "\\notepad.exe");

            IProcessStarter processStarter = new Aria2cProcessStarter(fakeFileFinder.Object)
            {
                DownloadedFilesDirPath = Assembly.GetExecutingAssembly().Location
            };

            processStarter.Run();

            Thread.Sleep(1000);

            processStarter.Exit();
        }
    }
}
