using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Aria4net.Client;
using Aria4net.Common;
using Aria4net.Server;
using NLog;
using RestSharp;

namespace Aria4net.Sample
{
    class Program
    {
        private static void Main(string[] args)
        {
            string appRoot = @"C:\work\aria4net";

            IDictionary<string, Aria2cResult<string>> downloadHistory = new Dictionary<string, Aria2cResult<string>>();
            var logger = LogManager.GetCurrentClassLogger();

            var config = new Aria2cConfig
                {
                    Executable = Path.Combine(appRoot, "tools\\aria2-1.16.3-win-32bit-build1\\aria2c.exe"),
                    Id = Guid.NewGuid().ToString(),
                    JsonrpcUrl = "http://localhost:6868/jsonrpc",
                    JsonrpcVersion = "2.0",
                    WebSocketUrl = "ws://localhost:6868/jsonrpc",
                    Port = 7000,
                    RpcPort = 6868
                };

            IServer server = new Aria2cServer(
                new Aria2cProcessStarter(
                    new Aria2cFinder(config), config) {DownloadedFilesDirPath = "c:\\temp"});

            server.Start();

            IClient client = new Aria2cJsonRpcClient(new RestClient(),
                                                     config,
                                                     downloadHistory,
                                                     new Aria2cWebSocketWatcher(config,
                                                                                logger).Connect(),
                                                     logger);

            var url =
                "ftp://download.warface.levelupgames.com.br/Warface/Installer/Instalador_Client_LevelUp_1.0.34.006.torrent";

            var gid = "";

            client.DownloadProgress += (sender, e) =>
                {
                    if (gid == e.Gid)
                    {
                        Console.Clear();
                        Console.WriteLine(
                            "\rStatus {5} | Progress {0:N0} % | Speed {1:N2} Mb/s | Eta {2:N0} m | Downloaded {3:N2}  Mb | Remaining {6:N2} Mb | Total {4:N2} Mb",
                            e.Progress, e.Speed.ToMegaBytes(), new TimeSpan(0, 0, (int) e.Eta).TotalMinutes,
                            e.Downloaded.ToMegaBytes(), e.Total.ToMegaBytes(), e.Status,
                            (e.Total - e.Downloaded).ToMegaBytes());
                    }
                };

            
            gid = client.AddTorrent(url);

            Console.ReadKey();

            server.Stop();
        }
    }

    public static class DoubleExtensions
    {
        public static double ToMegaBytes(this double value)
        {
            return (value / 1024) / 1024;
        }
    }
}
