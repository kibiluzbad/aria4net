namespace Aria4net.Server
{
    public interface IServerValidationRunner
    {
        void Run();
        void AddRule(IServerValidationRule getRuleForJsonRpcPort);
    }
}