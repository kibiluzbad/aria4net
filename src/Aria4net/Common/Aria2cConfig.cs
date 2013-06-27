using System.Configuration;

namespace Aria4net.Common
{
    public class Aria2cConfig : ConfigurationSection
    {
        [ConfigurationProperty("executable" , IsRequired = true)]
        public string Executable 
        {
            get { return (string) this["executable"]; }
            set { this["executable"] = value; }
        }

        [ConfigurationProperty("jsonrpcUrl", DefaultValue = "http://localhost:6800/jsonrpc", IsRequired = false)]
        public string JsonrpcUrl
        {
            get { return (string)this["jsonrpcUrl"]; }
            set { this["jsonrpcUrl"] = value; }
        }

        [ConfigurationProperty("jsonrpcVersion", DefaultValue = "2.0", IsRequired = false)]
        public string JsonrpcVersion
        {
            get { return (string)this["jsonrpcVersion"]; }
            set { this["jsonrpcVersion"] = value; }
        }

        [ConfigurationProperty("id", IsRequired = true)]
        public string Id
        {
            get { return (string)this["id"]; }
            set { this["id"] = value; }
        }

        [ConfigurationProperty("webSocketUrl", DefaultValue = "ws://localhost:6800/jsonrpc", IsRequired = false)]
        public string WebSocketUrl
        {
            get { return (string)this["webSocketUrl"]; }
            set { this["webSocketUrl"] = value; }
        }

        [ConfigurationProperty("port", DefaultValue = "6881-6999", IsRequired = false)]
        public string Port
        {
            get { return (string)this["port"]; }
            set { this["port"] = value; }
        }

        [ConfigurationProperty("rpcPort", DefaultValue = "6800", IsRequired = false)]
        public int RpcPort
        {
            get { return (int)this["rpcPort"]; }
            set { this["rpcPort"] = value; }
        }

        [ConfigurationProperty("concurrentDownloads", DefaultValue = 5, IsRequired = false)]
        public int ConcurrentDownloads
        {
            get { return (int)this["concurrentDownloads"]; }
            set { this["concurrentDownloads"] = value; }
        }

        [ConfigurationProperty("maxDownloadLimit", DefaultValue = (long)0, IsRequired = false)]
        public long MaxDownloadLimit
        {
            get { return (long)this["maxDownloadLimit"]; }
            set { this["maxDownloadLimit"] = value; }
        }

        [ConfigurationProperty("maxUploadLimit", DefaultValue = (long)0, IsRequired = false)]
        public long MaxUploadLimit
        {
            get { return (long)this["maxUploadLimit"]; }
            set { this["maxUploadLimit"] = value; }
        }
    }
}