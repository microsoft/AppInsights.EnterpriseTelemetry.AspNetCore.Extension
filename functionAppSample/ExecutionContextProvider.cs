using Microsoft.Azure.WebJobs;
using AppInsights.EnterpriseTelemetry.Context.Background;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension
{
    public class ExecutionContextProvider : ICurrentExecutionContextProvider
    {
        private readonly ExecutionContext _executionContext;
        public ExecutionContextProvider(ExecutionContext executionContext)
        {
            _executionContext = executionContext;
        }
        public string GetCurrentExecutionContextId()
        {
            return _executionContext.InvocationId.ToString();
        }
    }
}
