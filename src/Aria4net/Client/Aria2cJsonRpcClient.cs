using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Aria4net.Common;
using Aria4net.Server;
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
        private readonly IDictionary<string,Aria2cResult<string>> _history;
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

            IRestResponse response = _restClient.Execute(CreateRequest("aria2.addUri", new List<string[]>
                {
                    new[] {url}
                }));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);
            
            _history.Add(url,result);

            _watcher.Subscribe("aria2.onDownloadPause", gid =>
                {
                    if (gid == result.Result)
                    {
                        _logger.Info("Download da url {0} com gid {1} pausado.", url, gid);
                        var status = GetStatus(gid);
                        if (null != DownloadPaused) DownloadPaused(this, new Aria2cClientEventArgs
                        {
                            Gid = gid,
                            Progress = status.Progress,
                            Total = status.TotalLength,
                            Speed = status.DownloadSpeed,
                            Downloaded = status.CompletedLength,
                            Status = status.Status,
                            Files =  status.Files.Select(c=>c.Path).ToArray(),
                            Url = url
                        });
                    }
                });

            _watcher.Subscribe("aria2.onDownloadStop", gid =>
                {
                    if (gid == result.Result)
                    {
                        _history.Remove(url);
                        _logger.Info("Download da url {0} com gid {1} parado e removido.", url, gid);
                        if (null != DownloadStoped) DownloadStoped(this, new Aria2cClientEventArgs
                        {
                            Gid = gid,
                            Progress = 0,
                            Url = url
                        });
                    }
                });

            _watcher.Subscribe("aria2.onDownloadStart", gid =>
            {
                if (gid == result.Result)
                {
                    _logger.Info("Download da url {0} com gid {1} iniciado", url, gid);
                    var eventArgs =
                    new Aria2cClientEventArgs
                    {
                        Downloaded = 0,
                        Eta = 0,
                        Gid = gid,
                        Progress = 0,
                        Speed = 0,
                        Url = url
                    };
                    if (null != DownloadStarted) DownloadStarted.Invoke(this, eventArgs);
                    StartReportingProgress(eventArgs);
                }
            });
            _watcher.Subscribe("aria2.onDownloadComplete", gid =>
                {
                    if (gid == result.Result)
                    {
                        _logger.Info("Download da url {0} com gid {1} concluido", url, gid);
                        _history.Remove(url);

                        if (null != DownloadCompleted) DownloadCompleted(this, new Aria2cClientEventArgs
                        {
                            Gid = gid,
                            Progress = 100,
                            Url = url
                        });
                    }
                });

            return result.Result;
        }

        public virtual string AddTorrent(string url)
        {
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.addTorrent", GetTorrent(url)));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            _history.Add(url, result);
            
            _watcher.Subscribe("aria2.onDownloadPause", gid =>
                {
                    if (gid == result.Result)
                    {
                        _logger.Info("Download da url {0} com gid {1} pausado.", url, gid);
                        var status = GetStatus(gid);
                        if (null != DownloadPaused) DownloadPaused(this, new Aria2cClientEventArgs
                        {
                            Gid = gid,
                            Progress = status.Progress,
                            Total = status.TotalLength,
                            Speed = status.DownloadSpeed,
                            Downloaded = status.CompletedLength,
                            Status = status.Status,
                            Files =  status.Files.Select(c=>c.Path).ToArray(),
                            Url = url
                        });
                    }
                });

            _watcher.Subscribe("aria2.onDownloadStop", gid =>
                {
                    if (gid == result.Result)
                    {
                        _history.Remove(url);
                        _logger.Info("Download da url {0} com gid {1} parado e removido.", url, gid);
                        if (null != DownloadStoped) DownloadStoped(this, new Aria2cClientEventArgs
                        {
                            Gid = gid,
                            Progress = 0,
                            Url = url
                        });
                    }
                });

            _watcher.Subscribe("aria2.onDownloadStart", gid =>
                {
                    if (gid == result.Result)
                    {
                        _logger.Info("Download da url {0} com gid {1} iniciado.", url, gid);
                        var eventArgs =
                        new Aria2cClientEventArgs
                            {
                                Downloaded = 0,
                                Eta = 0,
                                Gid = gid,
                                Progress = 0,
                                Speed = 0,
                                Url = url
                            };
                        if (null != DownloadStarted) DownloadStarted.Invoke(this, eventArgs);
                        StartReportingProgress(eventArgs);
                    }
                });
            _watcher.Subscribe("aria2.onBtDownloadComplete", gid =>
            {
                if (gid == result.Result)
                {
                    _logger.Info("Download da url {0} com gid {1} concluido.", url, gid);
                    _history.Remove(url);
                    if (null != DownloadCompleted) DownloadCompleted(this, new Aria2cClientEventArgs
                        {
                            Gid = gid,
                            Progress = 100,
                            Url = url
                        });
                }
            });

            return result.Result;
        }

        public string Purge()
        {
            _logger.Info("Limpando downloads completos / com erro / removidos.");
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.purgeDownloadResult", ""));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            return result.Result;}

        public virtual Aria2cDownloadStatus GetStatus(string gid)
        {
            _logger.Info("Recuperando status de {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.tellStatus", gid));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<Aria2cDownloadStatus>>(response.Content);

            if(null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Pause(string gid)
        {
            _logger.Info("Pausando {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.forcePause", gid));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Resume(string gid)
        {
            _logger.Info("Reiniciando {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.unpause", gid));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Stop(string gid)
        {
            _logger.Info("Parando e removendo {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.forceRemove", gid));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        public virtual string Remove(string gid)
        {
            _logger.Info("Excluindo dados de {0}.", gid);
            IRestResponse response = _restClient.Execute(CreateRequest("aria2.removeDownloadResult", gid));

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Aria2cResult<string>>(response.Content);

            if (null != result.Error) throw new Aria2cException(result.Error.Code, result.Error.Message);

            return result.Result;
        }

        protected virtual void StartReportingProgress(Aria2cClientEventArgs eventArgs)
        {
            _logger.Info("Observando progresso de {0}.", eventArgs.Gid);

            var worker = new BackgroundWorker(){WorkerReportsProgress = true};
            worker.RunWorkerCompleted += (sender, args) => worker.Dispose();
            worker.DoWork += (sender, args) =>
                {
                    var result = new Aria2cDownloadStatus();
                    while (!result.Completed)
                    {
                        try
                        {
                            result = GetStatus(eventArgs.Gid);
                        }
                        catch (Aria2cException aex)
                        {
                            _logger.FatalException(aex.Message, aex);
                            break;
                        }

                        Thread.Sleep(500);

                        eventArgs.Eta = result.Eta;
                        eventArgs.Downloaded = result.CompletedLength;
                        eventArgs.Speed = result.DownloadSpeed;
                        eventArgs.Progress = result.Progress;
                        eventArgs.Total = result.TotalLength;
                        eventArgs.Files = result.Files.Select(c=>c.Path).ToArray();
                        eventArgs.Status = result.Status;

                        if (null != DownloadProgress) DownloadProgress.Invoke(this, eventArgs);
                    }
                };
            worker.RunWorkerAsync();

        }

        private string GetTorrent(string url)
        {
            using (var client = new WebClient())
            {

                var bytes = client.DownloadData(url);

                return Convert.ToBase64String(bytes);
            }
        }

        protected virtual IRestRequest CreateRequest(string method,IList<string[]> parameters)
        {
            var request = new RestRequest(_config.JsonrpcUrl)
                {
                    RequestFormat = DataFormat.Json
                };

            request.AddBody(new
                {
                    jsonrpc = _config.JsonrpcVersion,
                    id = _config.Id,
                    method,
                    @params = parameters
                });

            request.Method = Method.POST;

            return request;
        }

        protected virtual IRestRequest CreateRequest(string method, params string[] parameters)
        {
            var request = new RestRequest(_config.JsonrpcUrl)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddBody(new
            {
                jsonrpc = _config.JsonrpcVersion,
                id = _config.Id,
                method,
                @params = parameters
            });

            request.Method = Method.POST;

            return request;
        }

        public event EventHandler<Aria2cClientEventArgs> DownloadCompleted;
        public event EventHandler<Aria2cClientEventArgs> DownloadPaused;
        public event EventHandler<Aria2cClientEventArgs> DownloadStoped;
        public event EventHandler<Aria2cClientEventArgs> DownloadStarted;
        public event EventHandler<Aria2cClientEventArgs> DownloadProgress;
    }

    public class Aria2cDownloadStatus
    {
        public string Gid { get; set; }
        public string Status { get; set; }
        public double TotalLength { get; set; }
        public double CompletedLength { get; set; }
        public double UploadLength { get; set; }
        public string Bitfield { get; set; }
        public double DownloadSpeed { get; set; }
        public double UploadSpeed { get; set; }
        public string InfoHash { get; set; }
        public int? NumSeeders { get; set; }
        public string PieceLength { get; set; }
        public int? NumPieces { get; set; }
        public int? Connections { get; set; }
        public string ErrorCode { get; set; }
        public string[] FollowedBy { get; set; }
        public string BelongsTo { get; set; }
        public string Dir { get; set; }
        public IEnumerable<Aria2cFile> Files { get; set; }
       // public Aria2cBittorrent Bittorrent { get; set; }

        public bool Completed
        {
            get { return 0 < CompletedLength && CompletedLength == TotalLength; }
        }

        public double Progress
        {
            get { return CompletedLength * 100 / TotalLength; }
            
        }

        public double Remaining
        {
            get { return TotalLength - CompletedLength; }

        }

        public double Eta
        {
            get { return (TotalLength - CompletedLength) / DownloadSpeed; }
            
        }
    }

    public class Aria2cFile
    {
        public double CompletedLength { get; set; }
        public int Index { get; set; }
        public double Length { get; set; }
        public string Path { get; set; }
        public bool Selected { get; set; }

        public Aria2cUri Uri { get; set; }
    }

    public class Aria2cUri
    {
        public string Status { get; set; }
        public string Uri { get; set; }
    }

    public class Aria2cBittorrent
    {
        public string[] AnnounceList { get; set; }
        public string Comment { get; set; }
        public DateTime? CreationDate { get; set; }
        public string Mode { get; set; }
        public Aria2cInfo Info { get; set; }
    }

    public class Aria2cInfo
    {
        public string Name { get; set; }
    }

    public class Aria2cClientEventArgs : EventArgs
    {
        public string Gid { get; set; }
        public string Url { get; set; }
        public IList<string> Files { get; set; }
        public double Progress { get; set; }
        public double Eta { get; set; }
        public double Downloaded { get; set; }
        public double Speed { get; set; }
        public double Total { get; set; }
        public string Status { get; set; }
    }
}