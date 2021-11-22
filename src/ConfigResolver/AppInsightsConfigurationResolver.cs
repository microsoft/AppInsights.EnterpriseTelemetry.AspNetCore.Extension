using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsights.EnterpriseTelemetry.Configurations;
using Newtonsoft.Json;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension
{
    internal sealed class AppInsightsConfigurationResolver : IAppInsightsConfigurationResolver
    {
        private readonly IConfiguration _config;
        private readonly ITelemetryInitializer[] _customInitializers;
        private static ApplicationInsightsConfiguration _appInsightsConfiguration;

        public AppInsightsConfigurationResolver(IConfiguration config, params ITelemetryInitializer[] customInitializers)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _customInitializers = customInitializers;
        }

        public ApplicationInsightsConfiguration Resolve()
        {

            if (_appInsightsConfiguration != null)
                return _appInsightsConfiguration;

            string instrumentationKey = _config.GetValue<string>("ApplicationInsights:InstrumentationKey");
            instrumentationKey = string.IsNullOrWhiteSpace(instrumentationKey) ? _config.GetValue<string>("Logging:ApplicationInsights:InstrumentationKey") : instrumentationKey;

            string logLevel = _config.GetValue<string>("ApplicationInsights:TraceLevel");
            logLevel = string.IsNullOrWhiteSpace(logLevel) ? _config.GetValue<string>("Logging:LogLevel:Default") : logLevel;

            

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
                TelemetrySource = _config.GetValue<string>("Logging:Source"),

                RequestTelemetryEnhanced = _config.GetValue<bool>("Logging:RequestTelemetryEnhanced"),
                RequestBodyTrackingEnabled = _config.GetValue<bool>("Logging:RequestBodyTrackingEnabled"),
                ResponseBodyTrackingEnabled = _config.GetValue<bool>("Logging:ResponseBodyTrackingEnabled"),

                PropertySplittingEnabled = _config.GetValue<bool>("Logging:PropertySplittingEnabled"),
                ExceptionTrimmingEnabled = _config.GetValue<bool>("Logging:ExceptionTrimmingEnabled"),
                MaxPropertySize = _config.GetValue<int?>("Logging:MaxPropertySize") ?? TelemetryConstant.MAX_PROPERTY_SIZE,
                MaxExceptionDepth = _config.GetValue<int?>("Logging:MaxExceptionDepth") ?? TelemetryConstant.MAX_EXCEPTION_DEPTH,
                MaxMessageSize = _config.GetValue<int?>("Logging:MaxMessageSize") ?? TelemetryConstant.MAX_MESSAGE_SIZE
            };

            string redactedHeadersConfig = _config.GetValue<string>("Logging:RedactedHeaders") ?? "";
            List<string> redactedHeaders = redactedHeadersConfig.Split(',').ToList();
            appInsightsConfiguration.RedactedHeaders.AddRange(redactedHeaders);

            string excludedUrlsConfig = _config.GetValue<string>("Logging:ExcludedUrls") ?? "";
            List<string> excludedUrls = excludedUrlsConfig.Split(',').ToList();
            appInsightsConfiguration.ExcludedRequestUrls.AddRange(excludedUrls);

            string excludedHeadersConfig = _config.GetValue<string>("Logging:ExcludedHeaders");
            Dictionary<string, string> excludedHeaders = !string.IsNullOrWhiteSpace(excludedHeadersConfig) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(excludedHeadersConfig) : new Dictionary<string, string>();
            appInsightsConfiguration.ExcludedRequestHeaders = excludedHeaders;

            IEnumerable<IConfigurationSection> customTrackingProperties = _config.GetSection("Logging:Properties:Custom").GetChildren();
            if (customTrackingProperties != null && customTrackingProperties.Any())
            {
                foreach (IConfigurationSection customProperty in customTrackingProperties)
                {
                    appInsightsConfiguration.CustomTrackingProperties.AddOrUpdate(customProperty.Key, customProperty.Value);
                }
            }

            IEnumerable<IConfigurationSection> staticProperties = _config.GetSection("Logging:Properties:Static").GetChildren();
            if (staticProperties != null && staticProperties.Any())
            {
                foreach(IConfigurationSection staticProperty in staticProperties)
                {
                    appInsightsConfiguration.StaticProperties.AddOrUpdate(staticProperty.Key, staticProperty.Value);
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
