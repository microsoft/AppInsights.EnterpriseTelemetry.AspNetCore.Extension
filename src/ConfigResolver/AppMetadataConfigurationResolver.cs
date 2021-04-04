using System;
using Microsoft.Extensions.Configuration;
using AppInsights.EnterpriseTelemetry.Configurations;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension
{
    internal class AppMetadataConfigurationResolver : IAppMetadataConfigurationResolver
    {
        private readonly IConfiguration _config;

        public AppMetadataConfigurationResolver(IConfiguration configuration)
        {
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        public AppMetadataConfiguration Resolve()
        {
            return new AppMetadataConfiguration()
            {
                TenantIdHeaderKey = _config.GetValue<string>("Application:TenantIdHeaderKey") ?? _config.GetValue<string>("ItTelemetryExtensions:TenantKey") ?? TelemetryConstant.HEADER_DEFAULT_TENANT_ID,
                CorrelationIdHeaderKey = _config.GetValue<string>("Application:CorrelationIdHeaderKey") ?? _config.GetValue<string>("ItTelemetryExtensions:CorrelationKey") ?? TelemetryConstant.HEADER_DEFAULT_CORRELATION_KEY,
                SubCorrIdHeaderKey = _config.GetValue<string>("Application:SubCorrIdHeaderKey") ?? _config.GetValue<string>("ItTelemetryExtensions:SubCorrelationKey") ?? TelemetryConstant.HEADER_DEFAULT_SUB_CORRELATION_KEY,
                TransactionIdHeaderKey = _config.GetValue<string>("Application:TransactionIdHeaderKey") ?? _config.GetValue<string>("ItTelemetryExtensions:TransactionKey") ?? TelemetryConstant.HEADER_DEFAULT_TRANSACTION_KEY,
                EndToEndTrackingHeaderKey = _config.GetValue<string>("Application:EndToEndTrackingHeaderKey") ?? _config.GetValue<string>("ItTelemetryExtensions:EndToEndKey") ?? TelemetryConstant.HEADER_DEFAULT_E2E_KEY,
                BusinessProcessHeaderKey = _config.GetValue<string>("Application:BusinessProcessHeaderKey") ?? _config.GetValue<string>("ItTelemetryExtensions:BusinessProcessKey") ?? TelemetryConstant.HEADER_DEFAULT_BUSINESS_PROCESS_NAME
            };
        }
    }
}
