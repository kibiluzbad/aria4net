﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Threading;
using System.Xml.Serialization;
using Aria4net.Common;
using Aria4net.Exceptions;
using Aria4net.Server;
using Aria4net.Server.Watcher;
using AustinHarris.JsonRpc;
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
        private readonly IServerWatcher _watcher;
        private readonly Logger _logger;

        public Aria2cJsonRpcClient(IRestClient restClient,
                                   Aria2cConfig config,
                                   IServerWatcher watcher,
                                   Logger logger)
        {
            _restClient = restClient;
            _config = config;
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

                                               try
                                               {
                                                   Remove(args.Status.Gid);
                                               }
                                               catch (Aria2cException aex)
                                               {
                                                   _logger.ErrorException(aex.Message, aex);
                                               }

                                               if (null != DownloadCompleted)
                                                   DownloadCompleted(this, args);
                                           },
                                       OnError,
                                       OnStoped,
                                       OnPaused);

            string response = CreateRequest("aria2.addUri", new List<string[]>
                {
                    new[] {url}
                });
            

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            newGid = result.Result;

            return newGid;
        }

        protected virtual void OnProgress(Aria2cClientEventArgs args)
        {
            if (null != DownloadProgress) DownloadProgress(this, args);
        }

        protected virtual void OnPaused(Aria2cClientEventArgs args)
        {
            _logger.Info("Download da url {0} com gid {1} pausado.", args.Url, args.Status.Gid);

            if (null != DownloadPaused)
                DownloadPaused(this, args);
        }

        protected virtual void OnStoped(Aria2cClientEventArgs args)
        {
            _logger.Info("Download da url {0} com gid {1} parado e removido.", args.Url, args.Status.Gid);

            if (null != DownloadStoped)
                DownloadStoped(this, args);
        }

        protected virtual void OnError(Aria2cClientEventArgs args)
        {
            Remove(args.Status.Gid);

            _logger.Debug("Download da url {0} com gid {1} com erro.", args.Url, args.Status.Gid);

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

                                               try
                                               {
                                                   Remove(args.Status.Gid);
                                               }
                                               catch (Aria2cException aex)
                                               {
                                                   _logger.DebugException(aex.Message, aex);
                                               }

                                               if (null != token) token.Dispose();

                                               AddTorrentFile(torrentPath);
                                           },
                                       error: args =>
                                           {
                                               _logger.Info("Download da url {0} com gid {1} com erro. Código {2}",
                                                            args.Url, args.Status.Gid, args.Status.ErrorCode);

                                               Remove(args.Status.Gid);

                                               if (null != DownloadError)
                                                   DownloadError(this, args);
                                           });

            string response = CreateRequest("aria2.addUri", new List<string[]>
                {
                    new[] {url}
                });
            
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            newGid = result.Result;

            return newGid;
        }

        public virtual string AddTorrentFile(string path)
        {

            byte[] torrent = GetTorrent(path);
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

                                       try
                                       {
                                           Stop(args.Status.Gid);
                                           Remove(args.Status.Gid);
                                       }
                                       catch (Aria2cException aex)
                                       {
                                           _logger.ErrorException(aex.Message,aex);
                                       }

                                       if (null != DownloadCompleted)
                                           DownloadCompleted(this, args);
                                   },
                               OnError,
                               OnStoped,
                               OnPaused);

            string response = CreateRequest("aria2.addTorrent", new[] {Convert.ToBase64String(torrent)});

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            newGid = result.Result;

            return newGid;
        }

        public string Purge()
        {
            _logger.Info("Limpando downloads completos / com erro / removidos.");
            string response = CreateRequest<string>("aria2.purgeDownloadResult");

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            return result.Result;
        }

        public string Shutdown()
        {
            _logger.Info("Solicitando shutdown do servidor.");
            string response = CreateRequest<string>("aria2.forceShutdown");

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual Aria2cDownloadStatus GetStatus(string gid)
        {
            _logger.Info("Recuperando status de {0}.", gid);
            string response = CreateRequest("aria2.tellStatus", new[] {gid});

            var result =
                Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<Aria2cDownloadStatus>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual Aria2cDownloadStatus GetProgress(string gid)
        {
            var parameteres = new List<object>
                {
                    gid,
                    new[]
                        {
                            "status",
                            "completedLength",
                            "totalLength",
                            "downloadSpeed"
                        }
                };

            

            string response = CreateRequest("aria2.tellStatus",parameteres);

            var result =
                Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<Aria2cDownloadStatus>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);


            return result.Result;
        }

        public virtual string Pause(string gid)
        {
            _logger.Info("Pausando {0}.", gid);
            string response = CreateRequest("aria2.forcePause", new[] {gid});

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Resume(string gid)
        {
            _logger.Info("Reiniciando {0}.", gid);
            string response = CreateRequest("aria2.unpause", new[] {gid});

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Stop(string gid)
        {
            _logger.Info("Parando e removendo {0}.", gid);
            string response = CreateRequest("aria2.forceRemove", new[] {gid});

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Remove(string gid)
        {
            _logger.Info("Excluindo dados de {0}.", gid);
            string response = CreateRequest("aria2.removeDownloadResult", new[] {gid});

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        private byte[] GetTorrent(string path)
        {
            return File.ReadAllBytes(path);
        }

        protected virtual string CreateRequest<TParameters>(string method,
                                                                  TParameters parameters = default(TParameters))
        {
            var jsonrequest = new JsonObject();
			jsonrequest["id"] = _config.Id;
			jsonrequest["method"] = method;
            if (null != parameters)
            jsonrequest["params"] = parameters;

			var webRequest = (HttpWebRequest)WebRequest.Create( _config.JsonrpcUrl );
			webRequest.Method = "POST";

			TextWriter writer = new StreamWriter( webRequest.GetRequestStream());
			writer.Write( jsonrequest.ToString() );
			writer.Close();

            try
            {
                WebResponse response = webRequest.GetResponse();

                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                throw new Aria2cException((int)wex.Status, wex.Message, wex);
            }
            
        }

        public event EventHandler<Aria2cClientEventArgs> DownloadCompleted;
        public event EventHandler<Aria2cClientEventArgs> DownloadPaused;
        public event EventHandler<Aria2cClientEventArgs> DownloadError;
        public event EventHandler<Aria2cClientEventArgs> DownloadStoped;
        public event EventHandler<Aria2cClientEventArgs> DownloadStarted;
        public event EventHandler<Aria2cClientEventArgs> DownloadProgress;
    }
}