using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Threading;
using Aria4net.Common;
using Aria4net.Exceptions;
using Aria4net.Server;
using Aria4net.Server.Watcher;
using NLog;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers;

namespace Aria4net.Client
{
    public class Aria2cJsonRpcClient : IClient
    {
        private readonly IRestClient _restClient;
        private readonly Aria2cConfig _config;
        private readonly IDictionary<string, Aria2cResult<string>> _history;
        private readonly IServerWatcher _watcher;
        private readonly Logger _logger;

        public Aria2cJsonRpcClient(IRestClient restClient,
                                   Aria2cConfig config,
                                   IDictionary<string, Aria2cResult<string>> history,
                                   IServerWatcher watcher,
                                   Logger logger)
        {
            _restClient = restClient;
            _config = config;
            _history = history;
            _watcher = watcher;
            _logger = logger;
        }

        public virtual string AddUrl(string url)
        {
            _logger.Info("Adicionando url {0}", url);

            string newGid = string.Empty;
            IDisposable token = null;

            token = _watcher.Subscribe(() => newGid,
                                       gid => new Aria2cClientEventArgs
                                           {
                                               Url = url,
                                               Status = GetStatus(gid)
                                           },
                                       GetProgress,
                                       OnStarted,
                                       OnProgress,
                                       args =>
                                           {
                                               _logger.Info("Download da url {0} com gid {1} concluido.", args.Url,
                                                            args.Status.Gid);

                                               if (null != token) token.Dispose();

                                               if (null != DownloadCompleted)
                                                   DownloadCompleted(this, args);
                                           },
                                       OnError,
                                       OnStoped,
                                       OnPaused);

            IRestResponse response = _restClient.Execute(CreateRequest("aria2.addUri", new List<string[]>
                {
                    new[] {url}
                }));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            newGid = result.Result;

            return newGid;
        }

        protected virtual void OnProgress(Aria2cClientEventArgs args)
        {
            DownloadProgress(this, args);
        }

        protected virtual void OnPaused(Aria2cClientEventArgs args)
        {
            _logger.Info("Download da url {0} com gid {1} pausado.", args.Url, args.Status.Gid);

            if (null != DownloadPaused)
                DownloadPaused(this, args);
        }

        protected virtual void OnStoped(Aria2cClientEventArgs args)
        {
            _history.Remove(args.Url);
            _logger.Info("Download da url {0} com gid {1} parado e removido.", args.Url, args.Status.Gid);

            if (null != DownloadStoped)
                DownloadStoped(this, args);
        }

        protected virtual void OnError(Aria2cClientEventArgs args)
        {
            Remove(args.Status.Gid);

            _logger.Error("Download da url {0} com gid {1} com erro.", args.Url, args.Status.Gid);

            if (null != DownloadError)
                DownloadError(this, args);
        }

        protected virtual void OnStarted(Aria2cClientEventArgs args)
        {
            _logger.Info("Download da url {0} com gid {1} iniciado", args.Url, args.Status.Gid);

            if (null != DownloadStarted) DownloadStarted.Invoke(this, args);
        }

        protected virtual Aria2cClientEventArgs GetProgress(Aria2cClientEventArgs args)
        {
            var progress = GetProgress(args.Status.Gid);

            args.Status.DownloadSpeed = progress.DownloadSpeed;
            args.Status.CompletedLength = progress.CompletedLength;
            args.Status.Status = progress.Status;
            args.Status.TotalLength = progress.TotalLength;

            return args;
        }

        public virtual string AddTorrent(string url)
        {
            string newGid = string.Empty;
            IDisposable token = null;

            token = _watcher.Subscribe(() => newGid,
                                       gid => new Aria2cClientEventArgs
                                           {
                                               Status = GetStatus(gid),
                                               Url = url
                                           },
                                       completed: args =>
                                           {
                                               _logger.Info(
                                                   "Download do arquivo torrent url {0} com gid {1} concluido.",
                                                   args.Url, args.Status.Gid);

                                               string torrentPath = args.Status.Files.FirstOrDefault().Path;

                                               Remove(args.Status.Gid);

                                               if (null != token) token.Dispose();

                                               AddTorrent(GetTorrent(torrentPath), torrentPath);
                                           },
                                       error: args =>
                                           {
                                               _logger.Info("Download da url {0} com gid {1} com erro. Código {2}",
                                                            args.Url, args.Status.Gid, args.Status.ErrorCode);

                                               Remove(args.Status.Gid);

                                               if (null != DownloadError)
                                                   DownloadError(this, args);
                                           });

            IRestResponse response = _restClient.Execute(CreateRequest("aria2.addUri", new List<string[]>
                {
                    new[] {url}
                }));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            newGid = result.Result;

            return newGid;
        }

        public virtual string AddTorrent(byte[] torrent, string path)
        {
            string newGid = string.Empty;
            IDisposable token = null;

            _watcher.Subscribe(() => newGid,
                               gid => new Aria2cClientEventArgs
                                   {
                                       Url = path,
                                       Status = GetStatus(gid)
                                   },
                               GetProgress,
                               OnStarted,
                               OnProgress,
                               args =>
                                   {
                                       _logger.Info("Download da url {0} com gid {1} concluido.", args.Url,
                                                    args.Status.Gid);

                                       if (null != token) token.Dispose();

                                       if (null != DownloadCompleted)
                                           DownloadCompleted(this, args);
                                   },
                               OnError,
                               OnStoped,
                               OnPaused);

            IRestResponse response =
                _restClient.Execute(CreateRequest("aria2.addTorrent", new[] {Convert.ToBase64String(torrent)}));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            newGid = result.Result;

            return newGid;
        }

        public string Purge()
        {
            _logger.Info("Limpando downloads completos / com erro / removidos.");
            IRestResponse response = _restClient.Execute(CreateRequest<string>("aria2.purgeDownloadResult"));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            return result.Result;
        }

        public string Shutdown()
        {
            _logger.Info("Solicitando shutdown do servidor.");
            IRestResponse response = _restClient.Execute(CreateRequest<string>("aria2.forceShutdown"));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual Aria2cDownloadStatus GetStatus(string gid)
        {
            _logger.Info("Recuperando status de {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.tellStatus", new[] {gid}));

            var result =
                Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<Aria2cDownloadStatus>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual Aria2cDownloadStatus GetProgress(string gid)
        {
            _logger.Info("Recuperando progresso de {0}.", gid);

            IRestResponse response = _restClient.Execute(CreateRequest("aria2.tellStatus", new List<object>
                {
                    gid,
                    new[]
                        {
                            "status",
                            "completedLength",
                            "totalLength",
                            "downloadSpeed"
                        }
                }));

            var result =
                Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<Aria2cDownloadStatus>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);


            return result.Result;
        }

        public virtual string Pause(string gid)
        {
            _logger.Info("Pausando {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.forcePause", new[] {gid}));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Resume(string gid)
        {
            _logger.Info("Reiniciando {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.unpause", new[] {gid}));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Stop(string gid)
        {
            _logger.Info("Parando e removendo {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.forceRemove", new[] {gid}));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Remove(string gid)
        {
            _logger.Info("Excluindo dados de {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.removeDownloadResult", new[] {gid}));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        private byte[] GetTorrent(string path)
        {
            return File.ReadAllBytes(path);
        }

        protected virtual IRestRequest CreateRequest<TParameters>(string method,
                                                                  TParameters parameters = default(TParameters))
        {
            var request = new RestRequest(_config.JsonrpcUrl)
                {
                    RequestFormat = DataFormat.Json
                };

            if (null != parameters)
                request.AddBody(new
                    {
                        jsonrpc = _config.JsonrpcVersion,
                        id = _config.Id,
                        method,
                        @params = parameters
                    });
            else
                request.AddBody(new
                    {
                        jsonrpc = _config.JsonrpcVersion,
                        id = _config.Id,
                        method,
                    });

            request.Method = Method.POST;

            return request;
        }

        public event EventHandler<Aria2cClientEventArgs> DownloadCompleted;
        public event EventHandler<Aria2cClientEventArgs> DownloadPaused;
        public event EventHandler<Aria2cClientEventArgs> DownloadError;
        public event EventHandler<Aria2cClientEventArgs> DownloadStoped;
        public event EventHandler<Aria2cClientEventArgs> DownloadStarted;
        public event EventHandler<Aria2cClientEventArgs> DownloadProgress;
    }
}