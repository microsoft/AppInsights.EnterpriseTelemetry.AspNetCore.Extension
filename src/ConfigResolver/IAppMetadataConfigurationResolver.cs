using AppInsights.EnterpriseTelemetry.Configurations;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension
{
    public interface IAppMetadataConfigurationResolver
    {
        AppMetadataConfiguration Resolve();
    }
}