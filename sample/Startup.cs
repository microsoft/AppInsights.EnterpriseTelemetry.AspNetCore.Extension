using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppInsights.EnterpriseTelemetry.Web.Extension;
using AppInsights.EnterpriseTelemetry.Web.Extension.Filters;
using AppInsights.EnterpriseTelemetry.Web.Extension.Middlewares;

namespace AppInsights.EnterpriseTelemetry.AspNetCore.Extension.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEnterpriseLogger(Configuration, new CustomStaticInitializer());
            services.AddSingleton<IGlobalExceptionHandler, CustomExceptionHandler>();
            services.AddMvc(options =>
            {
                options.Filters.Add<TrackingPropertiesFilterAttribute>();
                options.EnableEndpointRouting = false;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Application Insights Enterprise Telemetry Sample",
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Email = "pratikb@microsoft.com",
                        Name = "Pratik Bhattacharya",
                        Url = new System.Uri("https://github.com/microsoft/AppInsights.EnterpriseTelemetry.AspNetCore.Extension")
                    },
                    Description = "Sample project to demonstrate Enterprise Telemetry using AppInsights.EnterpriseTelemetry.AspNetCoreExtension"
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flighting Service V2");
                c.RoutePrefix = string.Empty;
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseEnterpriseTelemetry(Configuration, new CustomStaticInitializer());
            app.UseHttpsRedirection();
            // app.UseSecurityMiddleware(Configuration);
            app.UseMvc();
        }
    }
}
