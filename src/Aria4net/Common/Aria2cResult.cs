namespace Aria4net.Common
{
// ReSharper disable InconsistentNaming
    public class Aria2cResult<TResult>
// ReSharper restore InconsistentNaming
    {
        public string Id { get; set; }
        public string Jsonrpc { get; set; }
        public TResult Result { get; set; }
        public Aria2cError Error { get; set; }
    }
}