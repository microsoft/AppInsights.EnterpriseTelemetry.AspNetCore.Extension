using System;
using Microsoft.AspNetCore.Http;

namespace AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares
{
    public interface IGlobalExceptionHandler
    {
        void Handle(Exception exception, HttpContext httpContext, string correlationId, string transactionId);
    }
}
