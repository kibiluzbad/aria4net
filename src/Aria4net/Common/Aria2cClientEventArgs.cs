using System;
using Aria4net.Client;

namespace Aria4net.Common
{
    public class Aria2cClientEventArgs : EventArgs
    {
        public Aria2cDownloadStatus Status { get; set; }

        public string Url { get; set; }
    }
}