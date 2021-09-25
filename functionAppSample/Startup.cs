using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppInsights.EnterpriseTelemetry.Web.Extension;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AppInsights.EnterpriseTelemetry.AspNetCore.Extension.FunctionAppSample.Startup))]

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension.FunctionAppSample
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();
            builder.ConfigurationBuilder
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{context.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(sp => builder.GetContext().Configuration);
            builder.Services.AddEnterpriseTelemetry(builder.GetContext().Configuration);
            builder.Services.AddSingleton(EnterpriseTelemetryExtensions.CreateLogger(builder.GetContext().Configuration));
        }
    }
}
