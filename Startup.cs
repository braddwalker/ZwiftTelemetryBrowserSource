using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZwiftPacketMonitor;
using ZwiftTelemetryBrowserSource.Models;
using Lib.AspNetCore.ServerSentEvents;
using ZwiftTelemetryBrowserSource.Services;

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
            services.AddTransient<INotificationsService, LocalNotificationsService>();
            services.AddServerSentEvents<INotificationsServerSentEventsService, NotificationsServerSentEventsService>(options =>
            {
                options.ReconnectInterval = 5000;
            });

            services.Configure<ZonesModel>(Configuration.GetSection("Zones"));        
            services.AddSingleton<Monitor>();

            // Since this is a background service, we also need to inject it into other services
            services.AddSingleton<AverageTelemetryService>();
            services.AddHostedService<AverageTelemetryService>(provider => provider.GetService<AverageTelemetryService>());

            services.AddHostedService<ZwiftMonitorService>();
            services.AddControllersWithViews();
            services.AddLogging(builder => 
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapServerSentEvents<NotificationsServerSentEventsService>("/notifications");
            });
        }
    }
}