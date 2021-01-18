using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZwiftPacketMonitor;
using ZwiftTelemetryBrowserSource.Models;
using Lib.AspNetCore.ServerSentEvents;
using ZwiftTelemetryBrowserSource.Services;
using ZwiftTelemetryBrowserSource.Services.Speech;
using ZwiftTelemetryBrowserSource.Services.Notifications;
using System.Linq;

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
            services.AddTransient<IRideOnNotificationService, RideOnNotificationService>();
            services.AddServerSentEvents<INotificationsServerSentEventsService, NotificationsServerSentEventsService>(options =>
            {
                options.ReconnectInterval = 5000;
            });
            services.AddServerSentEvents<IRideOnNotificationsSSEService, RideOnNotificationsSSEService>(options =>
            {
                options.ReconnectInterval = 5000;
            });

            services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "text/event-stream" });
            });

            services.Configure<ZonesModel>(Configuration.GetSection("Zones"));
            services.Configure<SpeechOptions>(Configuration.GetSection("Speech"));
            services.AddTransient<SpeechService>();
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
                endpoints.MapServerSentEvents<RideOnNotificationsSSEService>("/rideon");
            });
        }
    }
}