using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddTransient<Monitor>();
            services.AddHostedService<ZwiftMonitorService>();
            services.AddControllersWithViews();
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