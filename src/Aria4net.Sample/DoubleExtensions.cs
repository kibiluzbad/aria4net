namespace Aria4net.Sample
{
    public static class DoubleExtensions
    {
        public static double ToMegaBytes(this double value)
        {
            return (value / 1024) / 1024;
        }
    }
}