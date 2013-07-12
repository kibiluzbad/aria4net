using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aria4net.Common
{
    public class WindowsPathTokenizer : ITokenizer
    {
        public IEnumerable<string> Execute(string path)
        {
            return from Match match
                       in
                       Regex.Matches(path, @"(?<token>[^\\]+)(\\+)?", RegexOptions.Compiled | RegexOptions.Singleline)
                   where match.Success
                   select match.Groups["token"].Value;
        }
    }
}