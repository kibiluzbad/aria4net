namespace Aria4net.Client
{
    public class Aria2cFile
    {
        public double CompletedLength { get; set; }
        public int Index { get; set; }
        public double Length { get; set; }
        public string Path { get; set; }
        public bool Selected { get; set; }

        public Aria2cUri Uri { get; set; }
    }
}