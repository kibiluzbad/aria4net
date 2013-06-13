using System.Collections.Generic;

namespace Aria4net.Server
{
    public class DefaultValidationRunner : IServerValidationRunner
    {
        private ICollection<IServerValidationRule> _rules;
        
        public DefaultValidationRunner()
        {
            _rules = new HashSet<IServerValidationRule>();
        }
        
        public void Run()
        {
            foreach (var rule in _rules)
            {
                rule.Execute();
            }
        }

        public void AddRule(IServerValidationRule rule)
        {
            _rules.Add(rule);
        }
    }
}