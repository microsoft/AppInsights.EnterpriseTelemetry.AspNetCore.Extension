using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using AppInsights.EnterpriseTelemetry.Context;

namespace AppInsights.EnterpriseTelemetry.Web.Extension.Filters
{
    public sealed class RequestResponseLoggerFilterAttribute : ActionFilterAttribute
    {
        private readonly ILogger _logger;
        private readonly bool _isVerboseLoggingEnabled;
        private readonly bool _isHttpContextBodyLoggingEnabled;
        private readonly string _correlationIdHeaderKey;
        private readonly string _transactionIdHeaderKey;
        private readonly string _endToEndHeaderKey;

        public RequestResponseLoggerFilterAttribute(ILogger logger, IConfiguration config)
            :this(logger,
                        config.GetValue<string>("Logging:LogLevel:Default") == "Trace" || config.GetValue<string>("Logging:LogLevel:Default") == "Debug",
                        config.GetValue<bool>("Logging:EnableHttpContextBodyLogging"),
                        config.GetValue<string>("Application:CorrelationIdHeaderKey"),
                        config.GetValue<string>("Application:TransactionIdHeaderKey"),
                        config.GetValue<string>("Application:EndToEndTrackingHeaderKey"))
        {   
        }

        private RequestResponseLoggerFilterAttribute(ILogger logger, bool isVerboseLoggingEnabled, bool isHttpContextBodyLoggingEnabled, string correlationIdHeaderKey, string transactionIdHeaderKey, string e2eTrackingIdHeaderKey)
        {
            _logger = logger;
            _isVerboseLoggingEnabled = isVerboseLoggingEnabled;
            _isHttpContextBodyLoggingEnabled = isHttpContextBodyLoggingEnabled;
            _correlationIdHeaderKey = correlationIdHeaderKey;
            _transactionIdHeaderKey = transactionIdHeaderKey;
            _endToEndHeaderKey = e2eTrackingIdHeaderKey;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                if (!_isVerboseLoggingEnabled)
                    return;

                var correlationId = GetCorrelationId(context.HttpContext);
                var transactionId = GetTransactionId(context.HttpContext);

                var request = context.HttpContext.Request;
                var logProperties = GetLogPropertiesFromRequest(request);

                var messageContext = new MessageContext($"[Additional Request Log] {context.HttpContext.Request.Method} {context.HttpContext.Request.Path.Value}",
                    TraceLevel.Verbose, correlationId, transactionId, "RequestResponseLoggerFilter.OnActionExecuting", "System", "N/A");
                messageContext.AddProperties(logProperties);

                _logger.Log(messageContext);
            }
            catch (Exception exception)
            {
                Debug.Write($"UNHANDLED EXCEPTION IN TELEMETRY: {exception.ToString()}");
                // DO-NOTING. HTTP FLOW SHOULD NOT BREAK FOR FAILURE TO LOG
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                if (!_isVerboseLoggingEnabled)
                    return;

                var correlationId = GetCorrelationId(context.HttpContext);
                var transactionId = GetTransactionId(context.HttpContext);
                var endToEndTrackingId = GetEndToEndId(context.HttpContext);
                var logRequestProperties = GetLogPropertiesFromRequest(context.HttpContext.Request);
                var logResponseProperties = GetLogPropertiesFromResponse(context.HttpContext.Response);

                if (_isHttpContextBodyLoggingEnabled)
                {
                    var responseBody = GetResponseBody(context.Result);
                    logResponseProperties.AddOrUpdate("Response:Body", responseBody);
                }

                var messageContext = new MessageContext($"[Additional Response Log] {context.HttpContext.Request.Method} {context.HttpContext.Request.Path.Value}",
                    TraceLevel.Verbose, correlationId, transactionId, "RequestResponseLoggerFilter.OnResultExecuted", "System", endToEndTrackingId);
                messageContext.AddProperties(logRequestProperties);
                messageContext.AddProperties(logResponseProperties);

                _logger.Log(messageContext);
            }
            catch (Exception exception)
            {
                Debug.Write($"UNHANDLED EXCEPTION IN TELEMETRY: {exception.ToString()}");
                // DO-NOTING. HTTP FLOW SHOULD NOT BREAK FOR FAILURE TO LOG
            }
        }

        private string GetResponseBody(IActionResult result)
        {
            if (result == null)
                return TelemetryConstant.NO_RESPONSE;
            if (result is ObjectResult)
            {
                var responseObject = (result as ObjectResult).Value;
                if (responseObject is string)
                    return responseObject as string;
                return JsonConvert.SerializeObject(responseObject);
            }
            return result.ToString();
        }

        private Dictionary<string, string> GetLogPropertiesFromRequest(HttpRequest request)
        {
            var logProperties = new Dictionary<string, string>();

            foreach (var header in request.Headers)
            {
                if (header.Key == "Authorization")
                    logProperties.AddOrUpdate($"Request:Header:{header.Key}", TelemetryConstant.REDACTED);
                else
                    logProperties.AddOrUpdate($"Request:Header:{header.Key}", string.Join(",", header.Value));
            }
            logProperties.AddOrUpdate("Request:Method",      request.Method);
            logProperties.AddOrUpdate("Request:Protocol",    request.Protocol);
            logProperties.AddOrUpdate("Request:Scheme",      request.Scheme);
            logProperties.AddOrUpdate("Request:Host",        request.Host.Value);
            logProperties.AddOrUpdate("Request:Path",        request.Path.Value);
            logProperties.AddOrUpdate("Request:QueryString", request.QueryString.HasValue ? request.QueryString.Value : string.Empty);

            if (request.Method != HttpMethod.Get.ToString() && _isHttpContextBodyLoggingEnabled)
            {
                var requestBodyCopy = request.Body;
                request.EnableBuffering();

                var requestBodyStream = new MemoryStream();
                request.Body.CopyTo(requestBodyStream);
                requestBodyStream.Seek(0, SeekOrigin.Begin);
                var requestBodyStr = new StreamReader(requestBodyStream).ReadToEnd();
                request.Body = requestBodyCopy;
                logProperties.AddOrUpdate("Request:Body", requestBodyStr);
            }

            return logProperties;
        }

        private Dictionary<string, string> GetLogPropertiesFromResponse(HttpResponse response)
        {
            var logProperties = new Dictionary<string, string>();

            foreach (var header in response.Headers)
            {
                logProperties.AddOrUpdate($"Response:Header:{header.Key}", string.Join(",", header.Value));
                Console.WriteLine($"Response:Header:{header.Key} - " + string.Join(",", header.Value));
            }
            logProperties.AddOrUpdate("Response:StatusCode", response.StatusCode.ToString());
            return logProperties;
        }

        private string GetCorrelationId(HttpContext httpContext)
        {
            if ((httpContext.Request.Headers.TryGetValue(_correlationIdHeaderKey, out var values)))
            {
                return values.FirstOrDefault();
            }
            return Guid.NewGuid().ToString();
        }

        private string GetTransactionId(HttpContext httpContext)
        {
            if ((httpContext.Request.Headers.TryGetValue(_transactionIdHeaderKey, out var values)))
            {
                return values.FirstOrDefault();
            }
            return Guid.NewGuid().ToString();
        }

        private string GetEndToEndId(HttpContext httpContext)
        {
            if ((httpContext.Request.Headers.TryGetValue(_endToEndHeaderKey, out var values)))
            {
                return values.FirstOrDefault();
            }
            return Guid.NewGuid().ToString();
        }
    }
}
