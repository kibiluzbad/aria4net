using System;
using System.Collections.Generic;

namespace Aria4net.Client
{
    public class Aria2cClientEventArgs : EventArgs
    {
        public Aria2cDownloadStatus Status { get; set; }

        public string Url { get; set; }
    }
}