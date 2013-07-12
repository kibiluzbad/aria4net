using System.Collections.Generic;

namespace Aria4net.Server.Validation
{
    public class DefaultValidationRunner : IServerValidationRunner
    {
        private readonly ICollection<IServerValidationRule> _rules;

        public DefaultValidationRunner()
        {
            _rules = new HashSet<IServerValidationRule>();
        }

        public void Run()
        {
            foreach (IServerValidationRule rule in _rules)
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