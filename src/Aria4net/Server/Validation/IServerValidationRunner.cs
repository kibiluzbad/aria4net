namespace Aria4net.Server.Validation
{
    public interface IServerValidationRunner
    {
        void Run();
        void AddRule(IServerValidationRule getRuleForJsonRpcPort);
    }
}