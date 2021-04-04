using System;
using Microsoft.AspNetCore.Http;

namespace AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares
{
    /// <summary>
    /// Unhandled exception handler
    /// </summary>
    public interface IGlobalExceptionHandler
    {
        /// <summary>
        /// Gets called for each unhandled exception in the pipeline
        /// </summary>
        /// <param name="exception" cref"Exception">Unhandled exception</param>
        /// <param name="httpContext" cref="HttpContext"></param>
        /// <param name="correlationId">Correlation ID of the operation</param>
        /// <param name="transactionId">Transaction ID of the operation</param>
        void Handle(Exception exception, HttpContext httpContext, string correlationId, string transactionId);
    }
}
