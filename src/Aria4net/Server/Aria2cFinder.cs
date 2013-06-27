using System.IO;
using Aria4net.Common;

namespace Aria4net.Server
{
    public class Aria2cFinder : IFileFinder
    {
        private readonly Aria2cConfig _config;
        private readonly IPathFormatter _formatter;

        public Aria2cFinder(Aria2cConfig config, IPathFormatter formatter = null)
        {
            _config = config;
            _formatter = formatter ?? new DefaultPathFormatter();
        }

        public string Find()
        {
            var path = _formatter.Format(_config.Executable);

            if(!File.Exists(path)) 
                throw new FileNotFoundException("Não foi possível encontrar o executavel do aria2c",Path.GetFileName(path));

            return path;
        }
    }
}