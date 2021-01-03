using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using ZwiftPacketMonitor;

namespace ZwiftTelemetryBrowserSource
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ZwiftTelemetry>();
            services.AddSingleton<Monitor>();
            services.AddHostedService<ZwiftMonitorService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Map("/hr", r => r.UseMiddleware<HeartRateRequest>());
        }
    }
}