using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;

#pragma warning disable CA1031 // Do not catch general exception types
namespace AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares
{
    /// <summary>
    /// ASP.NET Core middleware to handle unhandled exception in the request pipeline
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly List<IGlobalExceptionHandler> _customHandlers;

        public readonly string _correlationIdHeaderKey;
        public readonly string _transactionIdHeaderKey;

        public ExceptionMiddleware(RequestDelegate next, ILogger logger, string correlationIdHeaderKey, string transactionIdHeaderKey, List<IGlobalExceptionHandler> customExceptionHandlers)
        {
            _next = next;
            _logger = logger;
            _correlationIdHeaderKey = correlationIdHeaderKey;
            _transactionIdHeaderKey = transactionIdHeaderKey;
            _customHandlers = customExceptionHandlers;
        }

        public ExceptionMiddleware(RequestDelegate next, ILogger logger, string correlationIdHeaderKey, string transactionIdHeaderKey)
            :this(next, logger, correlationIdHeaderKey, transactionIdHeaderKey, null)
        { }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception exception)
            {
                if (_customHandlers != null && _customHandlers.Any())
                {
                    foreach (var customHandler in _customHandlers)
                        customHandler.Handle(exception, httpContext, GetCorrelationId(httpContext.Request), GetTransactionId(httpContext.Request));
                }
                else
                {
                    DefaultHandler(exception, httpContext.Response, httpContext.Request);
                }
            }
        }
        

        private void DefaultHandler(Exception exception, HttpResponse response, HttpRequest request)
        {
            var correlationId = GetCorrelationId(request);
            var transactionId = GetTransactionId(request);

            response.Clear();
            response.Headers.Add(_correlationIdHeaderKey, correlationId);
            response.Headers.Add(_transactionIdHeaderKey, transactionId);
            
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = "Unhandled Exception occurred";
            response.WriteAsync($"OOPS! Something went wrong. Please contact support with tracking ID {correlationId}.").Wait();

            _logger.Log(exception, correlationId, transactionId, source: "ExceptionMiddleware");
        }

        private string GetCorrelationId(HttpRequest request)
        {
            if (request.Headers.ContainsKey(_correlationIdHeaderKey))
            {
                var correlationId = request.Headers[_correlationIdHeaderKey].ToString();
                correlationId = string.IsNullOrEmpty(correlationId) ? Guid.NewGuid().ToString() : correlationId;
                return correlationId;
            }
            return Guid.NewGuid().ToString();
        }

        private string GetTransactionId(HttpRequest request)
        {
            if (request.Headers.ContainsKey(_transactionIdHeaderKey))
            {
                var transactionId = request.Headers[_transactionIdHeaderKey].ToString();
                transactionId = string.IsNullOrEmpty(transactionId) ? Guid.NewGuid().ToString() : transactionId;
                return transactionId;
            }
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Extension method used to add the middleware to the HTTP request pipeline.
    /// </summary>
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionMiddleware(this IApplicationBuilder builder, string correlationIdHeaderKey, string transactionIdHeaderKey)
        {   
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
