using AppInsights.EnterpriseTelemetry.Context;
using AppInsights.EnterpriseTelemetry.Context.Background;
using Microsoft.Azure.WebJobs;
using System;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension.FunctionAppSample
{
    public class SampleTopicLogger
    {
        private readonly ILogger _logger;

        public SampleTopicLogger(ILogger logger)
        {
            _logger = logger;
        }

        [FunctionName("TestLogger")]
        public void Run([ServiceBusTrigger("%ServiceBus:MessageTopic%", "%ServiceBus:TelemetrySubscriber%", Connection = "ServiceBus:ConnectionString")] string message,
            ExecutionContext executionContext)
        {
            string correlationId = Guid.NewGuid().ToString();
            string transactionId = Guid.NewGuid().ToString();
            var contextProvider = new ExecutionContextProvider(executionContext);
            BackgroundContext.AddCurrentContext(contextProvider, correlationId, executionContext.FunctionName, "N/A", transactionId, "System", "SYS");

            // Log a message
            var msgContext = new MessageContext("A message from service bus has been received");
            _logger.Log(msgContext);

            // Log an Event
            var evtContext = new EventContext("Test.DummyEvent");
            evtContext.Properties.Add("Message", message);
            _logger.Log(evtContext);

            // Log a dependency
            var dependencyContext = new DependencyContext("SAMPLE", "https://test.com", "HTTP", message);
            dependencyContext.CompleteDependency("200", "Sample Response");
            _logger.Log(dependencyContext);
        }
    }
}
