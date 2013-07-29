using System;
using System.Collections.Generic;
using Aria4net.Common;

namespace Aria4net.Client
{
    public interface IClient
    {
        string AddUrl(string url);
        string AddTorrent(string url);
        string AddTorrentFile(string path, int[] indexes = null);
        string Pause(string gid);
        string Resume(string gid);
        string Stop(string gid);
        string Remove(string gid);
        string Purge();
        string Shutdown();

        Aria2cDownloadStatus GetStatus(string gid);
        IEnumerable<Aria2cFile> GetFiles(string gid);

        event EventHandler<Aria2cClientEventArgs> DownloadCompleted;
        event EventHandler<Aria2cClientEventArgs> DownloadPaused;
        event EventHandler<Aria2cClientEventArgs> DownloadError;
        event EventHandler<Aria2cClientEventArgs> DownloadStoped;
        event EventHandler<Aria2cClientEventArgs> DownloadStarted;
        event EventHandler<Aria2cClientEventArgs> DownloadProgress;

        
    }
}