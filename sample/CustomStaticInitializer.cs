using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension.Sample
{
    public class CustomStaticInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            ((ISupportProperties)telemetry).Properties.AddOrUpdate("Custom_Field_1", "Custom_Value_1");
            ((ISupportProperties)telemetry).Properties.AddOrUpdate("Custom_Field_2", "Custom_Value_2");
        }
    }
}
