using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Aria4net.Client;
using Aria4net.Common;
using Aria4net.Server;
using Microsoft.Isam.Esent.Collections.Generic;
using Moq;
using NLog;
using NUnit.Framework;
using Newtonsoft.Json;
using RestSharp;

namespace Aria4net.Tests
{
    [TestFixture]
    public class JsonRpcClientTests
    {
        [Test]
        public void When_calling_AddUrl_should_call_Execute_on_IRestClient()
        {
            const string url = "http://www.uol.com.br";
            const string jsonrpcVersion = "2.0";
            var sessionId = Guid.NewGuid().ToString();
            
            var mockRestClient = new Mock<IRestClient>();
            var fakeRestResponse = new Mock<IRestResponse<Aria2cResult<string>>>();
            var fakeDownloadQueue = new Mock<IDictionary<string, Aria2cResult<string>>>();
            var fakeServerWatcher = new Mock<IServerWatcher>();
            var fakeLogger = new Mock<Logger>();

            fakeRestResponse.Setup(c => c.StatusCode)
                .Returns(HttpStatusCode.OK);
            fakeRestResponse.Setup(c => c.Data)
                .Returns(new Aria2cResult<string> { Id = sessionId, Jsonrpc = jsonrpcVersion, Result = "2089b05ecca3d829" });

            mockRestClient.Setup(c => c.Execute<Aria2cResult<string>>(It.IsAny<IRestRequest>()))
                          .Returns(fakeRestResponse.Object);

            IClient client = new Aria2cJsonRpcClient(mockRestClient.Object,
                                               new Aria2cConfig
                                                   {
                                                       Id = sessionId,
                                                       JsonrpcUrl = "http://localhost:6800/jsonrpc",
                                                       JsonrpcVersion = jsonrpcVersion
                                                   },
                                               fakeDownloadQueue.Object,
                                               fakeServerWatcher.Object, 
                                               fakeLogger.Object);

            client.AddUrl(url);

            mockRestClient.Verify(c => c.Execute<Aria2cResult<string>>(It.IsAny<IRestRequest>()),
                                  Times.Once());
        }

        [Test]
        public void When_calling_AddUrl_should_add_Aria2cResult_to_download_history()
        {
            const string url = "http://www.uol.com.br";
            const string jsonrpcVersion = "2.0";
            var sessionId = Guid.NewGuid().ToString();

            var fakeRestClient = new Mock<IRestClient>();
            var fakeRestResponse = new Mock<IRestResponse<Aria2cResult<string>>>();
            var mockDownloadQueue = new Mock<IDictionary<string, Aria2cResult<string>>>();
            var fakeServerWatcher = new Mock<IServerWatcher>();
            var fakeLogger = new Mock<Logger>();

            fakeRestResponse.Setup(c => c.StatusCode)
                .Returns(HttpStatusCode.OK);
            fakeRestResponse.Setup(c => c.Data)
                .Returns(new Aria2cResult<string> { Id = sessionId, Jsonrpc = jsonrpcVersion, Result = "2089b05ecca3d829" });

            fakeRestClient.Setup(c => c.Execute<Aria2cResult<string>>(It.IsAny<IRestRequest>()))
                          .Returns(fakeRestResponse.Object);

            IClient client = new Aria2cJsonRpcClient(fakeRestClient.Object,
                                               new Aria2cConfig
                                               {
                                                   Id = sessionId,
                                                   JsonrpcUrl = "http://localhost:6800/jsonrpc",
                                                   JsonrpcVersion = jsonrpcVersion
                                               },
                                               mockDownloadQueue.Object,
                                               fakeServerWatcher.Object,
                                               fakeLogger.Object);

            client.AddUrl(url);

            mockDownloadQueue.Verify(c => c.Add(It.IsAny<string>(), It.IsAny<Aria2cResult<string>>()),
                                 Times.Once());
        }

        [Test]
        public void When_calling_AddUrl_should_Subscribe_to_recive_messages_from_IServerWatcher()
        {
            const string url = "http://www.uol.com.br";
            const string jsonrpcVersion = "2.0";
            var sessionId = Guid.NewGuid().ToString();

            var fakeRestClient = new Mock<IRestClient>();
            var fakeRestResponse = new Mock<IRestResponse<Aria2cResult<string>>>();
            var fakeDownloadQueue = new Mock<IDictionary<string, Aria2cResult<string>>>();
            var mockServerWatcher = new Mock<IServerWatcher>();
            var fakeLogger = new Mock<Logger>();

            fakeRestResponse.Setup(c => c.StatusCode)
                .Returns(HttpStatusCode.OK);
            fakeRestResponse.Setup(c => c.Data)
                .Returns(new Aria2cResult<string> { Id = sessionId, Jsonrpc = jsonrpcVersion, Result = "2089b05ecca3d829" });

            fakeRestClient.Setup(c => c.Execute<Aria2cResult<string>>(It.IsAny<IRestRequest>()))
                          .Returns(fakeRestResponse.Object);

            IClient client = new Aria2cJsonRpcClient(fakeRestClient.Object,
                                               new Aria2cConfig
                                               {
                                                   Id = sessionId,
                                                   JsonrpcUrl = "http://localhost:6800/jsonrpc",
                                                   JsonrpcVersion = jsonrpcVersion
                                               },
                                               fakeDownloadQueue.Object,
                                               mockServerWatcher.Object,
                                               fakeLogger.Object);

            client.AddUrl(url);

            mockServerWatcher.Verify(c => c.Subscribe(It.IsAny<string>(), It.IsAny<Action<string>>()), 
                Times.Once());
        }
    }
}
