using System.Collections.Generic;

namespace Aria4net.Server
{
    public class Aria2cWebSocketMessage
    {
        public string Jsonrpc { get; set; }
        public string Method { get; set; }
        public IEnumerable<Aria2cParameter> Params { get; set; }
    }
}