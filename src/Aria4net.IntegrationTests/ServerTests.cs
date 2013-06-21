using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Aria4net.Client;
using Aria4net.Common;
using Aria4net.Server;
using Aria4net.Server.Validation;
using Aria4net.Server.Watcher;
using Moq;
using NLog;
using NUnit.Framework;
using RestSharp;

namespace Aria4net.IntegrationTests
{
    [TestFixture]
    public class ServerTests
    {
        [Test, Ignore]
        public void Start_stop_server()
        {
            string appRoot = @"C:\work\aria4net";
            var fakeWatcher = new Mock<IServerWatcher>();

            var logger = LogManager.GetCurrentClassLogger();

            var config = new Aria2cConfig
                {
                    Executable = Path.Combine(appRoot, "tools\\aria2-1.16.3-win-32bit-build1\\aria2c.exe")
                };

            IServer server = new Aria2cServer(
                new Aria2cProcessStarterWithWindow(
                    new Aria2cFinder(config), config, logger),
                    new DefaultValidationRunner(), 
                    config,
                    logger,
                    fakeWatcher.Object);

            server.Start();

            Thread.Sleep(1000);

            server.Stop();
        }

        [Test, Ignore]
        public void Add_download()
        {
            string appRoot = @"C:\work\aria4net";
            

            var logger = LogManager.GetCurrentClassLogger();
            
            

            var config = new Aria2cConfig
                {
                    Executable = Path.Combine(appRoot, "tools\\aria2-1.16.3-win-32bit-build1\\aria2c.exe"),
                    Id = Guid.NewGuid().ToString(),
                    JsonrpcUrl = "http://localhost:6800/jsonrpc",
                    JsonrpcVersion = "2.0",
                    WebSocketUrl = "ws://localhost:6800/jsonrpc"
                };

            var watcher = new Aria2cWebSocketWatcher(config, logger);

            IServer server = new Aria2cServer(
                new Aria2cProcessStarterWithWindow(
                    new Aria2cFinder(config), config, logger) {DownloadedFilesDirPath = "c:\\temp"},
                    new DefaultValidationRunner(), 
                    config,
                    logger,
                    watcher);

            server.Start();

            IClient client = new Aria2cJsonRpcClient(new RestClient(),
                                                     config,
                                                     watcher,
                                                     logger);

            client.AddTorrent(
                "ftp://download.warface.levelupgames.com.br/Warface/Installer/Instalador_Client_LevelUp_1.0.34.006.torrent");

            server.Stop();
        }

        [Test, Ignore]
        public void Get_status()
        {
            var config = new Aria2cConfig
            {
                Executable = "",
                Id = Guid.NewGuid().ToString(),
                JsonrpcUrl = "http://localhost:6800/jsonrpc",
                JsonrpcVersion = "2.0",
                Port = 7001,
                RpcPort = 6800,
                WebSocketUrl = "ws://localhost:6800/jsonrpc"
            };

            var logger = LogManager.GetCurrentClassLogger();
            
            IClient client = new Aria2cJsonRpcClient(new RestClient(),
                                                   config,
                                                   new Aria2cWebSocketWatcher(config,
                                                                              logger).Connect(),
                                                   logger);

            var status = client.GetStatus("6ad3263090c0ea45");
        }
    }
}
