using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsights.EnterpriseTelemetry.Configurations;
using AppInsights.EnterpriseTelemetry.AspNetCore.Extension;
using AppInsights.EnterpriseTelemetry.Web.Extension.Filters;
using AppInsights.EnterpriseTelemetry.AppInsightsInitializers;
using AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares;

namespace AppInsights.EnterpriseTelemetry.Web.Extension
{
    /// <summary>
    /// Extensions class to use Enterprise Logger in ASP.NET Core Applications
    /// </summary>
    public static class EnterpriseTelemetryExtensions
    {
        private static ILogger _logger;
        private static readonly object _lock = new object();

        /// <summary>
        /// Creates an instance of <see cref="ILogger"/>
        /// </summary>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        /// <param name="customTelemetryInitializer" cref="ITelemetryInitializer[]">App specific additional telemetry initializers</param>
        /// <returns cref="ILogger">Enterprise Logger</returns>
        public static ILogger CreateLogger(IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializer)
        {
            lock (_lock)
            {
                if (_logger != null)
                    return _logger;

                ApplicationInsightsConfiguration appInsightsConfiguration = AppInsightsConfigurationResolver.Get(config, customTelemetryInitializer).Resolve();
                AppMetadataConfiguration appMetadataConfiguration = AppMetadataConfigurationResolver.Get(config).Resolve();
                _logger = new ApplicationInsightsLogger(appInsightsConfiguration, appMetadataConfiguration);
                return _logger;
            }
        }

        /// <summary>
        /// Registers <see cref="ILogger"/> in <see cref="IServiceCollection"/> for dependency injection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        /// <param name="customTelemetryInitializer" cref="ITelemetryInitializer[]">App specific additional telemetry initializers</param>
        public static void AddEnterpriseLogger(this IServiceCollection services, IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializer)
        {
            services.AddSingleton(CreateLogger(config, customTelemetryInitializer));
            services.RegisterRequestResponseLoggingFilter(config);
            services.RegisterTrackingPropertiesFilter(config);
            services.AddApplicationInsightsTelemetry(config);
        }

        /// <summary>
        /// Registers the filter <see cref= "RequestResponseInitializer"/> for adding additional attributes in the Request/Response for HTTP request
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        public static void RegisterRequestResponseLoggingFilter(this IServiceCollection services, IConfiguration config)
        {   
            services.AddScoped(sp =>
            {
                return new RequestResponseLoggerFilterAttribute(_logger ?? CreateLogger(config), config);
            });
        }

        /// <summary>
        /// Registers the filter <see cref="TrackingPropertiesFilterAttribute"/> to add tracking IDs in each HTTP request and response
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        public static void RegisterTrackingPropertiesFilter(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped(sp =>
            {
                return new TrackingPropertiesFilterAttribute(config, _logger ?? CreateLogger(config));
            });
        }

        /// <summary>
        /// Adds enterprise telemetry in the request pipeline
        /// </summary>
        /// <param name="app" cref="IApplicationBuilder"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        /// <param name="customTelemetryInitializer" cref="ITelemetryInitializer[]">App specific additional telemetry initializers</param>
        public static void UseEnterpriseTelemetry(this IApplicationBuilder app, IConfiguration config, params ITelemetryInitializer[] customInitializers)
        {   
            TelemetryConfiguration defaultTelemetryConfig = app.ApplicationServices.GetService<TelemetryConfiguration>();
            ApplicationInsightsConfiguration appInsightsConfig = AppInsightsConfigurationResolver.Get(config, customInitializers).Resolve();
            AppMetadataConfiguration appMetadataConfig = AppMetadataConfigurationResolver.Get(config).Resolve();

            if (config.GetValue<bool>("Logging:RequestTelemetryEnhanced"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new RequestResponseInitializer(appInsightsConfig));

            if (config.GetValue<bool>("Logging:ClientSideErrorSuppressionEnabled"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new ClientSideErrorInitializer());

            if (config.GetValue<bool>("Logging:RequestResponseLoggingEnabled"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new ResponseCodeTranslationIntitializer());

            if (config.GetValue<bool>("Logging:AutoTrackingEnabled"))
                defaultTelemetryConfig.TelemetryInitializers.Add(new TrackingInitializer(appInsightsConfig, appMetadataConfig));

            if (customInitializers != null && customInitializers.Any())
            {
                foreach(var customInitializer in customInitializers)
                {
                    defaultTelemetryConfig.TelemetryInitializers.Add(customInitializer);
                }
            }

            UseExceptionMiddleware(app, config);
        }

        /// <summary>
        /// Adds a global Exception Handling Middleware in the request pipeline
        /// </summary>
        /// <param name="app"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        public static void UseExceptionMiddleware(this IApplicationBuilder app, IConfiguration config)
        {   
            var appConfiguration = AppMetadataConfigurationResolver.Get(config).Resolve();
            app.UseMiddleware<ExceptionMiddleware>(
                app.ApplicationServices.GetService<ILogger>(),
                appConfiguration.CorrelationIdHeaderKey, 
                appConfiguration.TransactionIdHeaderKey,
                app.ApplicationServices.GetServices<IGlobalExceptionHandler>().ToList());
        }
    }
}
