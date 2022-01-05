using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsights.EnterpriseTelemetry.Configurations;
using AppInsights.EnterpriseTelemetry.Web.Extension.Filters;
using AppInsights.EnterpriseTelemetry.AppInsightsProcessors;
using AppInsights.EnterpriseTelemetry.AppInsightsInitializers;
using AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension
{
    public class TelemetryExtensionsBuilder : ITelemetryExtensionsBuilder
    {
        protected readonly IConfiguration _configuration;
        protected readonly IAppInsightsConfigurationResolver _appInsightsConfigurationResolver;
        protected readonly IAppMetadataConfigurationResolver _appMetadataConfigurationResolver;
        protected readonly IList<ITelemetryInitializer> _telemetryInitializers;

        protected ILogger _logger;
        protected static readonly object _lock = new();

        public TelemetryExtensionsBuilder() { }

        public TelemetryExtensionsBuilder(IConfiguration configuration, params ITelemetryInitializer[] telemetryInitializers)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _appInsightsConfigurationResolver = new AppInsightsConfigurationResolver(configuration, telemetryInitializers);
            _appMetadataConfigurationResolver = new AppMetadataConfigurationResolver(configuration);
            _telemetryInitializers = telemetryInitializers ?? Array.Empty<ITelemetryInitializer>();
            CreateLogger();
        }

        public virtual ILogger CreateLogger()
        {
            lock (_lock)
            {
                if (_logger == null)
                {
                    ApplicationInsightsConfiguration appInsightsConfig = _appInsightsConfigurationResolver.Resolve();
                    AppMetadataConfiguration appMetadataConfig = _appMetadataConfigurationResolver.Resolve();
                    _logger = new ApplicationInsightsLogger(appInsightsConfig, appMetadataConfig);
                }
                return _logger;
            }
        }

        public ApplicationInsightsConfiguration GetAppInsightsConfig()
        {
            return _appInsightsConfigurationResolver.Resolve();
        }

        public AppMetadataConfiguration GetAppMetadataConfig()
        {
            return _appMetadataConfigurationResolver.Resolve();
        }

        public void AddEnterpriseTelemetry(IServiceCollection services)
        {
            ILogger logger = CreateLogger();
            services.AddSingleton(logger);
            services.AddSingleton(_appInsightsConfigurationResolver.Resolve());
            services.AddSingleton(_appMetadataConfigurationResolver.Resolve());
            AddTrackingFilter(services);
            AddRequestResponseFilter(services);
            services.AddApplicationInsightsTelemetry(_configuration);
            services.AddApplicationInsightsTelemetryProcessor<ExcludedRequestsFilter>();
        }

        public void AddTrackingFilter(IServiceCollection services)
        {
            ILogger logger = CreateLogger();
            services.AddScoped(sp =>
            {
                return new TrackingPropertiesFilterAttribute(_configuration, logger);
            });
        }

        public void AddRequestResponseFilter(IServiceCollection services)
        {
            ILogger logger = CreateLogger();
            services.AddScoped(sp =>
            {
                return new RequestResponseLoggerFilterAttribute(_configuration, logger);
            });
        }

        public void UseEnterpriseTelemetry(IApplicationBuilder app)
        {
            TelemetryConfiguration defaultTelemetryConfig = app.ApplicationServices.GetService<TelemetryConfiguration>();
            if (defaultTelemetryConfig != null)
            {
                if (_configuration.GetValue<bool>("Logging:RequestTelemetryEnhanced"))
                    defaultTelemetryConfig.TelemetryInitializers.Add(new RequestResponseInitializer(GetAppInsightsConfig()));

                if (_configuration.GetValue<bool>("Logging:ClientSideErrorSuppressionEnabled"))
                    defaultTelemetryConfig.TelemetryInitializers.Add(new ClientSideErrorInitializer());

                if (_configuration.GetValue<bool>("Logging:RequestResponseLoggingEnabled"))
                    defaultTelemetryConfig.TelemetryInitializers.Add(new ResponseCodeTranslationIntitializer());

                if (_configuration.GetValue<bool>("Logging:AutoTrackingEnabled"))
                    defaultTelemetryConfig.TelemetryInitializers.Add(new TrackingInitializer(GetAppInsightsConfig(), GetAppMetadataConfig()));

                if (_telemetryInitializers != null && _telemetryInitializers.Any())
                {
                    foreach (var customInitializer in _telemetryInitializers)
                    {
                        defaultTelemetryConfig.TelemetryInitializers.Add(customInitializer);
                    }
                }
            }

            UseExceptionMiddleware(app);
        }

        public void UseExceptionMiddleware(IApplicationBuilder app)
        {
            AppMetadataConfiguration appConfiguration = _appMetadataConfigurationResolver.Resolve();
            app.UseMiddleware<ExceptionMiddleware>(
                app.ApplicationServices.GetService<ILogger>(),
                appConfiguration.CorrelationIdHeaderKey,
                appConfiguration.TransactionIdHeaderKey,
                app.ApplicationServices.GetServices<IGlobalExceptionHandler>().ToList());
        }
    }
}
