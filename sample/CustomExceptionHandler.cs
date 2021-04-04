using System;
using Microsoft.AspNetCore.Http;
using AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension.Sample
{
    public class CustomExceptionHandler : IGlobalExceptionHandler
    {
        private readonly ILogger _logger;

        public CustomExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(Exception exception, HttpContext httpContext, string correlationId, string transactionId)
        {
            var customException = new Exception("THIS IS A CUSTOM ERROR", exception);
            _logger.Log(customException, correlationId, transactionId, source: "CustomExceptionHandler");
        }
    }
}
