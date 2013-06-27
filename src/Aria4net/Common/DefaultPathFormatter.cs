using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Aria4net.Common
{
    public class DefaultPathFormatter : IPathFormatter
    {
        private readonly ITokenizer _tokenizer;

        internal static IDictionary<string, Func<string, string>> Rules = new Dictionary<string, Func<string, string>>
            {
                {"{app}",value => Regex.Replace(value,"\\{app\\}", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))} 
            };

        public DefaultPathFormatter(ITokenizer tokenizer = null)
        {
            _tokenizer = tokenizer ?? new WindowsPathTokenizer();
        }

        public string Format(string path)
        {
            foreach (var key in _tokenizer.Execute(path).Where(token => Rules.ContainsKey(token)))
            {
                path = Rules[key](path);
            }

            return path;
        }
    }
}