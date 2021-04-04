using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

#pragma warning disable CA1031 // Do not catch general exception types
namespace AppInsights.EnterpriseTelemetry.Web.Extension.Filters
{
    /// <summary>
    /// ASP.NET Core filter to add tracking properties for each HTTP Request. Add Correlation ID, Sub-Correlation ID and Transaction ID
    /// </summary>
    public sealed class TrackingPropertiesFilterAttribute : ActionFilterAttribute
    {
        private readonly string _correlationIdHeaderKey;
        private readonly string _transactionIdHeaderKey;
        private readonly string _e2eTrackingIdHeaderKey;
        private readonly string _subCorrelationIdHeaderKey;
        private readonly ILogger _logger;

        private TrackingPropertiesFilterAttribute(string correlationIdHeaderKey, string transactionIdHeaderKey, string e2eTrackingIdHeaderKey, string subCorrelationIdHeaderKey, ILogger logger)
        {
            _correlationIdHeaderKey = correlationIdHeaderKey ?? TelemetryConstant.HEADER_DEFAULT_CORRELATION_KEY;
            _transactionIdHeaderKey = transactionIdHeaderKey ?? TelemetryConstant.HEADER_DEFAULT_TRANSACTION_KEY; 
            _e2eTrackingIdHeaderKey = e2eTrackingIdHeaderKey ?? TelemetryConstant.HEADER_DEFAULT_E2E_KEY; ;
            _subCorrelationIdHeaderKey = subCorrelationIdHeaderKey ?? TelemetryConstant.HEADER_DEFAULT_SUB_CORRELATION_KEY;
            _logger = logger;
        }

        public TrackingPropertiesFilterAttribute(IConfiguration config, ILogger logger)
            : this(
                    correlationIdHeaderKey: config.GetValue<string>("Application:CorrelationIdHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:CorrelationKey"),
                    transactionIdHeaderKey: config.GetValue<string>("Application:TransactionIdHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:TransactionKey"),
                    e2eTrackingIdHeaderKey: config.GetValue<string>("Application:EndToEndTrackingHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:EndToEndKey"),
                    subCorrelationIdHeaderKey: config.GetValue<string>("Application:SubCorrIdHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:SubCorrelationKey"),
                    logger: logger
                 )
        { }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                // Add/Update Correlation ID
                if (!context.HttpContext.Request.Headers.TryGetValue(_correlationIdHeaderKey, out _))
                {
                    var newCorrelationId = Guid.NewGuid().ToString();
                    context.HttpContext.Request.Headers.AddOrUpdate(_correlationIdHeaderKey, newCorrelationId);
                    _logger.Log($"Correlation ID {newCorrelationId} has been generated");
                }

                // Add/Update Transaction ID
                if (!context.HttpContext.Request.Headers.TryGetValue(_transactionIdHeaderKey, out _))
                {
                    var newTransactionId = Guid.NewGuid().ToString();
                    context.HttpContext.Request.Headers.AddOrUpdate(_transactionIdHeaderKey, newTransactionId);
                    _logger.Log($"Transaction ID {newTransactionId} has been generated");
                }

                // Add SUB-XCV
                var subCorrelationId = Guid.NewGuid().ToString();
                context.HttpContext.Request.Headers.AddOrUpdate(_subCorrelationIdHeaderKey, subCorrelationId);
                _logger.Log($"Sub-XCV ID {subCorrelationId} has been generated");
            }
            catch (Exception exception)
            {
                // DO not throw exception
                _logger.Log(new Exception("There was an error while trying to set the tracking context", exception));
            }

        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                if (context.HttpContext.Request.Headers.TryGetValue(_correlationIdHeaderKey, out var headerValues))
                {
                    correlationId = headerValues.FirstOrDefault();
                }
                if (!context.HttpContext.Response.Headers.TryGetValue(_correlationIdHeaderKey, out _))
                {
                    context.HttpContext.Response.Headers.AddOrUpdate(_correlationIdHeaderKey, correlationId);
                }

                if (string.IsNullOrWhiteSpace(_transactionIdHeaderKey))
                    return;

                var transactionId = Guid.NewGuid().ToString();
                if (context.HttpContext.Request.Headers.TryGetValue(_transactionIdHeaderKey, out headerValues))
                {
                    transactionId = headerValues.FirstOrDefault();
                }
                if (!context.HttpContext.Response.Headers.TryGetValue(_transactionIdHeaderKey, out _))
                {
                    context.HttpContext.Response.Headers.AddOrUpdate(_transactionIdHeaderKey, transactionId);
                }

                if (string.IsNullOrWhiteSpace(_e2eTrackingIdHeaderKey))
                    return;

                var e2eId = "N/A";
                if (context.HttpContext.Request.Headers.TryGetValue(_e2eTrackingIdHeaderKey, out headerValues))
                {
                    e2eId = headerValues.FirstOrDefault();
                }
                if (!context.HttpContext.Response.Headers.TryGetValue(_e2eTrackingIdHeaderKey, out _))
                {
                    context.HttpContext.Response.Headers.AddOrUpdate(_e2eTrackingIdHeaderKey, e2eId);
                }

                if (string.IsNullOrWhiteSpace(_subCorrelationIdHeaderKey))
                    return;

                var subCorrelationId = Guid.NewGuid().ToString();
                if (context.HttpContext.Request.Headers.TryGetValue(_subCorrelationIdHeaderKey, out headerValues))
                {
                    subCorrelationId = headerValues.FirstOrDefault();
                }
                if (!context.HttpContext.Response.Headers.TryGetValue(_subCorrelationIdHeaderKey, out _))
                {
                    context.HttpContext.Response.Headers.AddOrUpdate(_subCorrelationIdHeaderKey, subCorrelationId);
                }
            }
            catch (Exception exception)
            {
                // DO not throw exception
                _logger.Log(new Exception("There was an error while trying to set the tracking context in response body", exception));
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
