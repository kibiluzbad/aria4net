using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Aria4net.Client;
using Aria4net.Common;
using Aria4net.Server;
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


            IServer server = new Aria2cServer(
                new Aria2cProcessStarterWithWindow(
                    new Aria2cFinder(
                        new Aria2cConfig
                            {
                                Executable = Path.Combine(appRoot, "tools\\aria2-1.16.3-win-32bit-build1\\aria2c.exe")
                            })));

            server.Start();

            Thread.Sleep(1000);

            server.Stop();
        }

        [Test, Ignore]
        public void Add_download()
        {
            string appRoot = @"C:\work\aria4net";

            IDictionary<string, Aria2cResult<string>> downloadHistory = new Dictionary<string, Aria2cResult<string>>();
            var logger = LogManager.GetCurrentClassLogger();

            var config = new Aria2cConfig
                {
                    Executable = Path.Combine(appRoot, "tools\\aria2-1.16.3-win-32bit-build1\\aria2c.exe"),
                    Id = Guid.NewGuid().ToString(),
                    JsonrpcUrl = "http://localhost:6800/jsonrpc",
                    JsonrpcVersion = "2.0",
                    WebSocketUrl = "ws://localhost:6800/jsonrpc"
                };

            IServer server = new Aria2cServer(
                new Aria2cProcessStarterWithWindow(
                    new Aria2cFinder(config)) {DownloadedFilesDirPath = "c:\\temp"});

            server.Start();

            IClient client = new Aria2cJsonRpcClient(new RestClient(),
                                                     config,
                                                     downloadHistory,
                                                     new Aria2cWebSocketWatcher(config,
                                                                                logger).Connect(),
                                                     logger);

            client.AddTorrent(
                "ftp://download.warface.levelupgames.com.br/Warface/Installer/Instalador_Client_LevelUp_1.0.34.006.torrent");

            server.Stop();
        }
    }

    internal class Aria2cProcessStarterWithWindow : Aria2cProcessStarter
    {
        public Aria2cProcessStarterWithWindow(IFileFinder fileFinder) : base(fileFinder)
        {
        }

        protected override void ConfigureProcess(System.Diagnostics.Process process)
        {
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        }
    }
}
