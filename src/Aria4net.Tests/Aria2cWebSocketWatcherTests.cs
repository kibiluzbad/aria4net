using System.Text;
using Aria4net.Common;
using Aria4net.Server;
using Aria4net.Server.Watcher;
using Moq;
using NLog;
using NUnit.Framework;

namespace Aria4net.Tests
{
    [TestFixture]
    public class Aria2cWebSocketWatcherTests
    {
        //TODO: Update test
        [Test, Ignore]
        public void Can_subscribe_to_recieve_messages()
        {
            var config = new Aria2cConfig { WebSocketUrl = "ws://echo.websocket.org" };
            var fakeLogger = new Mock<Logger>();
            const string methodToWatch = "aria2.onDownloadStarted";
            
            var watcher = new Aria2cWebSocketWatcher(config,fakeLogger.Object)
                .Connect();
            
        }
    
    }
}
