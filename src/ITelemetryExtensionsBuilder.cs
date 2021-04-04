using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using AppInsights.EnterpriseTelemetry.Configurations;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension
{
    public interface ITelemetryExtensionsBuilder
    {
        ILogger CreateLogger();
        ApplicationInsightsConfiguration GetAppInsightsConfig();
        AppMetadataConfiguration GetAppMetadataConfig();

        void AddEnterpriseTelemetry(IServiceCollection services);
        void AddRequestResponseFilter(IServiceCollection services);
        void AddTrackingFilter(IServiceCollection services);

        void UseEnterpriseTelemetry(IApplicationBuilder app);
        void UseExceptionMiddleware(IApplicationBuilder app);
    }
}