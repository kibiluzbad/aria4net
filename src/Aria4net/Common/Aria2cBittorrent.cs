using System;

namespace Aria4net.Common
{
// ReSharper disable InconsistentNaming
    public class Aria2cBittorrent
// ReSharper restore InconsistentNaming
    {
        public string[] AnnounceList { get; set; }
        public string Comment { get; set; }
        public DateTime? CreationDate { get; set; }
        public string Mode { get; set; }
        public Aria2cInfo Info { get; set; }
    }
}