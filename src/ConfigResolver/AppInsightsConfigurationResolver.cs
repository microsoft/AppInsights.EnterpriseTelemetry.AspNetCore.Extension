using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsights.EnterpriseTelemetry.Configurations;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension
{
    internal sealed class AppInsightsConfigurationResolver : IAppInsightsConfigurationResolver
    {
        private readonly IConfiguration _config;
        private readonly ITelemetryInitializer[] _customInitializers;
        private static ApplicationInsightsConfiguration _appInsightsConfiguration;
        private static readonly object _lock = new object();
        private static AppInsightsConfigurationResolver _instance = null;

        private AppInsightsConfigurationResolver(IConfiguration config, params ITelemetryInitializer[] customInitializers)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _customInitializers = customInitializers;
        }

        public static AppInsightsConfigurationResolver Get(IConfiguration config, params ITelemetryInitializer[] customInitializers)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new AppInsightsConfigurationResolver(config, customInitializers);
                }
                return _instance;
            }
        }

        public ApplicationInsightsConfiguration Resolve()
        {

            if (_appInsightsConfiguration != null)
                return _appInsightsConfiguration;

            string instrumentationKey = _config.GetValue<string>("ApplicationInsights:InstrumentationKey");
            instrumentationKey = string.IsNullOrWhiteSpace(instrumentationKey) ? _config.GetValue<string>("Logging:ApplicationInsights:InstrumentationKey") : instrumentationKey;

            string logLevel = _config.GetValue<string>("ApplicationInsights:TraceLevel");
            logLevel = string.IsNullOrWhiteSpace(logLevel) ? _config.GetValue<string>("Logging:LogLevel:Default") : logLevel;

            string redactedHeadersConfig = _config.GetValue<string>("Logging:RedactedHeaders") ?? "";
            List<string> redactedHeaders = redactedHeadersConfig.Split(',').ToList();

            var appInsightsConfiguration = new ApplicationInsightsConfiguration()
            {
                InstrumentationKey = instrumentationKey,
                LogLevel = ParseTraceLevel(logLevel),
                AutoTrackingEnabled = _config.GetValue<bool>("Logging:AutoTrackingEnabled"),
                ClientSideErrorSuppressionEnabled = _config.GetValue<bool>("Logging:ClientSideErrorSuppressionEnabled"),
                EnvironmentInitializerEnabled = _config.GetValue<bool>("Logging:EnvironmentInitializerEnabled"),
                ResponseCodeTranslationEnabled = _config.GetValue<bool>("Logging:ResponseCodeTranslationEnabled"),

                CorrelationIdPropertyKey = _config.GetValue<string>("Logging:Properties:CorrelationId") ?? TelemetryConstant.CORRELATION_KEY,
                SubCorrelationIdPropertyKey = _config.GetValue<string>("Logging:Properties:SubCorrelationId") ?? TelemetryConstant.SUB_CORRELATION_KEY,
                EndToEndIdPropertyKey = _config.GetValue<string>("Logging:Properties:EndToEnd") ?? TelemetryConstant.E2E_KEY,
                TenantIdPropertyKey = _config.GetValue<string>("Logging:Properties:Tenant") ?? TelemetryConstant.TENANT_KEY,
                TransactionIdPropertyKey = _config.GetValue<string>("Logging:Properties:TransactionId") ?? TelemetryConstant.TRANSACTION_KEY,
                UserPropertyKey = _config.GetValue<string>("Logging:Properties:User") ?? TelemetryConstant.USER_KEY,
                BusinessProcessPropertyKey = _config.GetValue<string>("Logging:Properties:BusinessProcess") ?? TelemetryConstant.BUSINESS_PROCESS_KEY,

                RequestTelemetryEnhanced = _config.GetValue<bool>("Logging:RequestTelemetryEnhanced"),
                RequestBodyTrackingEnabled = _config.GetValue<bool>("Logging:RequestBodyTrackingEnabled"),
                ResponseBodyTrackingEnabled = _config.GetValue<bool>("Logging:ResponseBodyTrackingEnabled"),

                PropertySplittingEnabled = _config.GetValue<bool>("Logging:PropertySplittingEnabled"),
                ExceptionTrimmingEnabled = _config.GetValue<bool>("Logging:ExceptionTrimmingEnabled"),
                MaxPropertySize = _config.GetValue<int?>("Logging:MaxPropertySize") ?? TelemetryConstant.MAX_PROPERTY_SIZE,
                MaxExceptionDepth = _config.GetValue<int?>("Logging:MaxExceptionDepth") ?? TelemetryConstant.MAX_EXCEPTION_DEPTH,
                MaxMessageSize = _config.GetValue<int?>("Logging:MaxMessageSize") ?? TelemetryConstant.MAX_MESSAGE_SIZE
            };
            appInsightsConfiguration.RedactedHeaders.AddRange(redactedHeaders);

            var customTrackingProperties = _config.GetSection("Logging:Properties:Custom").GetChildren();
            if (customTrackingProperties != null && customTrackingProperties.Any())
            {
                foreach (var customProperty in customTrackingProperties)
                {
                    appInsightsConfiguration.CustomTrackingProperties.AddOrUpdate(customProperty.Key, customProperty.Value);
                }
            }

            if (_customInitializers != null && _customInitializers.Any())
                appInsightsConfiguration.CustomInitializers.AddRange(_customInitializers.ToList());

            _appInsightsConfiguration = appInsightsConfiguration;

            return _appInsightsConfiguration;
        }

        private static TraceLevel ParseTraceLevel(string traceLevel)
        {
            if (traceLevel.ToLowerInvariant() == "Critical".ToLowerInvariant())
                return TraceLevel.Critical;
            if (traceLevel.ToLowerInvariant() == "Error".ToLowerInvariant())
                return TraceLevel.Error;
            if (traceLevel.ToLowerInvariant() == "Warning".ToLowerInvariant())
                return TraceLevel.Warning;
            if (traceLevel.ToLowerInvariant() == "Information".ToLowerInvariant())
                return TraceLevel.Information;
            if (traceLevel.ToLowerInvariant() == "Trace".ToLowerInvariant())
                return TraceLevel.Information;
            return TraceLevel.Verbose;
        }
    }
}
