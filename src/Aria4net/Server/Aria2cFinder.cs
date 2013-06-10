using System.IO;
using Aria4net.Common;

namespace Aria4net.Server
{
    public class Aria2cFinder : IFileFinder
    {
        private readonly Aria2cConfig _config;

        public Aria2cFinder(Aria2cConfig config)
        {
            _config = config;
        }

        public string Find()
        {
            var path = _config.Executable;

            if(!File.Exists(path)) 
                throw new FileNotFoundException("Não foi possível encontrar o executavel do aria2c",Path.GetFileName(path));

            return path;
        }
    }
}