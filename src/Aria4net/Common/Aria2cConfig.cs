namespace Aria4net.Common
{
    public class Aria2cConfig
    {
        public string Executable { get; set; }

        public string JsonrpcUrl { get; set; }

        public string JsonrpcVersion { get; set; }

        public string Id { get; set; }

        public string WebSocketUrl { get; set; }

        public int Port { get; set; }

        public int RpcPort { get; set; }

        public string OtherParameters { get; set; }
    }
}