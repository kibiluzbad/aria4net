using System.Collections.Generic;
using Aria4net.Common;

namespace Aria4net.Server.Watcher
{
    public class Aria2cWebSocketMessage
    {
        public string Jsonrpc { get; set; }
        public string Method { get; set; }
        public IEnumerable<Aria2cParameter> Params { get; set; }
    }
}