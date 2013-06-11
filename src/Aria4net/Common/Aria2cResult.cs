namespace Aria4net.Common
{
    public class Aria2cResult<TResult>
    {
        public string Id { get; set; }
        public string Jsonrpc { get; set; }
        public TResult Result { get; set; }
        public Aria2cError Error { get; set; }
    }
}