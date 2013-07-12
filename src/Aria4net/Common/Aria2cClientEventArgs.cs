using System;

namespace Aria4net.Common
{
// ReSharper disable InconsistentNaming
    public class Aria2cClientEventArgs : EventArgs
// ReSharper restore InconsistentNaming
    {
        public Aria2cDownloadStatus Status { get; set; }

        public string Url { get; set; }
    }
}