using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsights.EnterpriseTelemetry.AspNetCore.Extension;
using AppInsights.EnterpriseTelemetry.Web.Extension.Filters;
using AppInsights.EnterpriseTelemetry.AppInsightsInitializers;

namespace AppInsights.EnterpriseTelemetry.Web.Extension
{
    /// <summary>
    /// Extensions class to use Enterprise Logger in ASP.NET Core Applications
    /// </summary>
    public static class EnterpriseTelemetryExtensions
    {   
        private static readonly object _lock = new object();
        private static ITelemetryExtensionsBuilder extensionBuilder;

        public static void SetExtensionsBuilder(ITelemetryExtensionsBuilder builder)
        {
            extensionBuilder = builder;
        }

        private static ITelemetryExtensionsBuilder GetBuilder(IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializers)
        {
            lock(_lock)
            {
                if (extensionBuilder != null)
                {
                    return extensionBuilder;
                }
                extensionBuilder = TelemetryExtensionsBuilder.Create(config, customTelemetryInitializers);
                return extensionBuilder;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="ILogger"/>
        /// </summary>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        /// <param name="customTelemetryInitializers" cref="ITelemetryInitializer[]">App specific additional telemetry initializers</param>
        /// <returns cref="ILogger">Enterprise Logger</returns>
        public static ILogger CreateLogger(IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializers)
        {
            ITelemetryExtensionsBuilder builder = GetBuilder(config, customTelemetryInitializers);
            return builder.CreateLogger();
        }

        /// <summary>
        /// Registers <see cref="ILogger"/> in <see cref="IServiceCollection"/> for dependency injection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        /// <param name="customTelemetryInitializer" cref="ITelemetryInitializer[]">App specific additional telemetry initializers</param>
        public static void AddEnterpriseLogger(this IServiceCollection services, IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializers)
        {
            ITelemetryExtensionsBuilder builder = GetBuilder(config, customTelemetryInitializers);
            builder.AddEnterpriseTelemetry(services);
        }

        /// <summary>
        /// Registers the filter <see cref= "RequestResponseInitializer"/> for adding additional attributes in the Request/Response for HTTP request
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        public static void RegisterRequestResponseLoggingFilter(this IServiceCollection services, IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializers)
        {
            ITelemetryExtensionsBuilder builder = GetBuilder(config, customTelemetryInitializers);
            builder.AddRequestResponseFilter(services);
        }

        /// <summary>
        /// Registers the filter <see cref="TrackingPropertiesFilterAttribute"/> to add tracking IDs in each HTTP request and response
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        public static void RegisterTrackingPropertiesFilter(this IServiceCollection services, IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializers)
        {
            ITelemetryExtensionsBuilder builder = GetBuilder(config, customTelemetryInitializers);
            builder.AddTrackingFilter(services);
        }

        /// <summary>
        /// Adds enterprise telemetry in the request pipeline
        /// </summary>
        /// <param name="app" cref="IApplicationBuilder"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        /// <param name="customTelemetryInitializer" cref="ITelemetryInitializer[]">App specific additional telemetry initializers</param>
        public static void UseEnterpriseTelemetry(this IApplicationBuilder app, IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializers)
        {
            ITelemetryExtensionsBuilder builder = GetBuilder(config, customTelemetryInitializers);
            builder.UseEnterpriseTelemetry(app);
        }

        /// <summary>
        /// Adds a global Exception Handling Middleware in the request pipeline
        /// </summary>
        /// <param name="app"></param>
        /// <param name="config" cref="IConfiguration">Configuration</param>
        public static void UseExceptionMiddleware(this IApplicationBuilder app, IConfiguration config, params ITelemetryInitializer[] customTelemetryInitializers)
        {
            ITelemetryExtensionsBuilder builder = GetBuilder(config, customTelemetryInitializers);
            builder.UseExceptionMiddleware(app);
        }
    }
}
