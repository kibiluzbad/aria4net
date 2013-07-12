using System.Collections.Generic;
using Aria4net.Common;

namespace Aria4net.Server.Watcher
{
// ReSharper disable InconsistentNaming
    public class Aria2cWebSocketMessage
// ReSharper restore InconsistentNaming
    {
        public string Jsonrpc { get; set; }
        public string Method { get; set; }
        public IEnumerable<Aria2cParameter> Params { get; set; }
    }
}