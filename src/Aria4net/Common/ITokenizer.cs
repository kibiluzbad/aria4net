using System.Collections.Generic;

namespace Aria4net.Common
{
    public interface ITokenizer
    {
        IEnumerable<string> Execute(string path);
    }
}