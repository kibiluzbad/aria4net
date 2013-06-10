using System;
using System.Collections.Generic;

namespace Aria4net.Client
{
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