using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsights.EnterpriseTelemetry.Configurations;
using AppInsights.EnterpriseTelemetry.Web.Extension.Filters;
using AppInsights.EnterpriseTelemetry.AppInsightsInitializers;
using AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares;

namespace AppInsights.EnterpriseTelemetry.Web.Extension
{
    public static class EnterpriseTelemetryExtensions
    {
        private static ILogger _logger;
        private static ApplicationInsightsConfiguration _appInsightsConfiguration;
        private static readonly object _lock = new object();

        private static ApplicationInsightsConfiguration CreateAppInsightsConfiguration(IConfiguration config, params ITelemetryInitializer[] customInitializers)
        {
            lock (_lock)
            {
                if (_appInsightsConfiguration != null)
                    return _appInsightsConfiguration;

                var instrumentationKey = config.GetValue<string>("ApplicationInsights:InstrumentationKey");
                instrumentationKey = string.IsNullOrWhiteSpace(instrumentationKey) ? config.GetValue<string>("Logging:ApplicationInsights:InstrumentationKey") : instrumentationKey;

                var logLevel = config.GetValue<string>("ApplicationInsights:TraceLevel");
                logLevel = string.IsNullOrWhiteSpace(logLevel) ? config.GetValue<string>("Logging:LogLevel:Default") : logLevel;

                var appInsightsConfiguration = new ApplicationInsightsConfiguration()
                {
                    InstrumentationKey = instrumentationKey,
                    LogLevel = ParseTraceLevel(logLevel),
                    AutoTrackingEnabled = config.GetValue<bool>("Logging:AutoTrackingEnabled"),
                    ClientSideErrorSuppressionEnabled = config.GetValue<bool>("Logging:ClientSideErrorSuppressionEnabled"),
                    EnvironmentInitializerEnabled = config.GetValue<bool>("Logging:EnvironmentInitializerEnabled"),
                    ResponseCodeTranslationEnabled = config.GetValue<bool>("Logging:ResponseCodeTranslationEnabled"),

                    CorrelationIdPropertyKey = config.GetValue<string>("Logging:Properties:CorrelationId") ?? TelemetryConstant.CORRELATION_KEY,
                    SubCorrelationIdPropertyKey = config.GetValue<string>("Logging:Properties:SubCorrelationId") ?? TelemetryConstant.SUB_CORRELATION_KEY,
                    EndToEndIdPropertyKey = config.GetValue<string>("Logging:Properties:EndToEnd") ?? TelemetryConstant.E2E_KEY,
                    TenantIdPropertyKey = config.GetValue<string>("Logging:Properties:Tenant") ?? TelemetryConstant.TENANT_KEY,
                    TransactionIdPropertyKey = config.GetValue<string>("Logging:Properties:TransactionId") ?? TelemetryConstant.TRANSACTION_KEY,
                    UserPropertyKey = config.GetValue<string>("Logging:Properties:User") ?? TelemetryConstant.USER_KEY,
                    BusinessProcessPropertyKey = config.GetValue<string>("Logging:Properties:BusinessProcess") ?? TelemetryConstant.BUSINESS_PROCESS_KEY,

                    RequestTelemetryEnhanced = config.GetValue<bool>("Logging:RequestTelemetryEnhanced"),
                    RequestBodyTrackingEnabled = config.GetValue<bool>("Logging:RequestBodyTrackingEnabled"),
                    ResponseBodyTrackingEnabled = config.GetValue<bool>("Logging:ResponseBodyTrackingEnabled"),

                    PropertySplittingEnabled = config.GetValue<bool>("Logging:PropertySplittingEnabled"),
                    ExceptionTrimmingEnabled = config.GetValue<bool>("Logging:ExceptionTrimmingEnabled"),
                    MaxPropertySize = config.GetValue<int?>("Logging:MaxPropertySize") ?? TelemetryConstant.MAX_PROPERTY_SIZE,
                    MaxExceptionDepth = config.GetValue<int?>("Logging:MaxExceptionDepth") ?? TelemetryConstant.MAX_EXCEPTION_DEPTH,
                    MaxMessageSize = config.GetValue<int?>("Logging:MaxMessageSize") ?? TelemetryConstant.MAX_MESSAGE_SIZE
                };

                var customTrackingProperties = config.GetSection("Logging:Properties:Custom").GetChildren();
                if (customTrackingProperties != null && customTrackingProperties.Any())
                {
                    foreach (var customProperty in customTrackingProperties)
                    {
                        appInsightsConfiguration.CustomTrackingProperties.AddOrUpdate(customProperty.Key, customProperty.Value);
                    }
                }

                if (customInitializers != null && customInitializers.Any())
                    appInsightsConfiguration.CustomInitializers.AddRange(customInitializers.ToList());

                _appInsightsConfiguration = appInsightsConfiguration;

                return _appInsightsConfiguration;
            }
        }

        private static AppMetadataConfiguration CreateAppConfiguration(IConfiguration config)
        {
            var configuration = new AppMetadataConfiguration()
            {   
                TenantIdHeaderKey = config.GetValue<string>("Application:TenantIdHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:TenantKey") ?? TelemetryConstant.HEADER_DEFAULT_TENANT_ID,
                CorrelationIdHeaderKey = config.GetValue<string>("Application:CorrelationIdHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:CorrelationKey") ?? TelemetryConstant.HEADER_DEFAULT_CORRELATION_KEY,
                SubCorrIdHeaderKey = config.GetValue<string>("Application:SubCorrIdHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:SubCorrelationKey") ?? TelemetryConstant.HEADER_DEFAULT_SUB_CORRELATION_KEY,
                TransactionIdHeaderKey = config.GetValue<string>("Application:TransactionIdHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:TransactionKey") ?? TelemetryConstant.HEADER_DEFAULT_TRANSACTION_KEY,
                EndToEndTrackingHeaderKey = config.GetValue<string>("Application:EndToEndTrackingHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:EndToEndKey") ?? TelemetryConstant.HEADER_DEFAULT_E2E_KEY,
                BusinessProcessHeaderKey = config.GetValue<string>("Application:BusinessProcessHeaderKey") ?? config.GetValue<string>("ItTelemetryExtensions:BusinessProcessKey") ?? TelemetryConstant.HEADER_DEFAULT_BUSINESS_PROCESS_NAME,
            };

            return configuration;
        }

        public static ILogger CreateLogger(IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializer)
        {
            lock (_lock)
            {
                if (_logger != null)
                    return _logger;

                var appInsightsConfiguration = CreateAppInsightsConfiguration(config, customTelemetryInitializer);
                var appMetadataConfiguration = CreateAppConfiguration(config);
                _logger = new ApplicationInsightsLogger(appInsightsConfiguration, appMetadataConfiguration);
                return _logger;
            }
        }

        public static void AddEnterpriseLogger(this IServiceCollection services, IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializer)
        {
            services.AddSingleton(CreateLogger(config, customTelemetryInitializer));
            services.RegisterRequestResponseLoggingFilter(config);
            services.RegisterTrackingPropertiesFilter(config);
            services.AddApplicationInsightsTelemetry(config);
        }

        public static void RegisterRequestResponseLoggingFilter(this IServiceCollection services, IConfiguration config)
        {   
            services.AddScoped(sp =>
            {
                return new RequestResponseLoggerFilterAttribute(_logger ?? CreateLogger(config), config);
            });
        }

        public static void RegisterTrackingPropertiesFilter(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped(sp =>
            {
                return new TrackingPropertiesFilterAttribute(config, _logger ?? CreateLogger(config));
            });
        }

        public static void UseEnterpriseTelemetry(this IApplicationBuilder app, IConfiguration config, params ITelemetryInitializer[] customInitializers)
        {   
            var defaultTelemetryConfig = app.ApplicationServices.GetService<TelemetryConfiguration>();

            if (config.GetValue<bool>("Logging:RequestTelemetryEnhanced"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new RequestResponseInitializer(CreateAppInsightsConfiguration(config)));

            if (config.GetValue<bool>("Logging:ClientSideErrorSuppressionEnabled"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new ClientSideErrorInitializer());

            if (config.GetValue<bool>("Logging:RequestResponseLoggingEnabled"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new ResponseCodeTranslationIntitializer());

            if (config.GetValue<bool>("Logging:AutoTrackingEnabled"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new TrackingInitializer(CreateAppInsightsConfiguration(config), CreateAppConfiguration(config)));

            if (customInitializers != null && customInitializers.Any())
            {
                foreach(var customInitializer in customInitializers)
                {
                    defaultTelemetryConfig.TelemetryInitializers.Add(customInitializer);
                }
            }

            UseExceptionMiddleware(app, config);
        }

        public static void UseExceptionMiddleware(this IApplicationBuilder app, IConfiguration config)
        {   
            var appConfiguration = CreateAppConfiguration(config);
            app.UseMiddleware<ExceptionMiddleware>(
                app.ApplicationServices.GetService<ILogger>(),
                appConfiguration.CorrelationIdHeaderKey, 
                appConfiguration.TransactionIdHeaderKey,
                app.ApplicationServices.GetServices<IGlobalExceptionHandler>().ToList());
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
