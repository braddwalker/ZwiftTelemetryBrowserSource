using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace ZwiftTelemetryBrowserSource
{
    public class ZwiftMonitorService : IHostedService
    {
        public ZwiftMonitorService(ILogger<ZwiftMonitorService> logger, 
            IHostApplicationLifetime applicationLifetime,
            ZwiftTelemetry zwiftTelemetry,
            ZwiftPacketMonitor.Monitor zwiftPacketMonitor) {

            Logger = logger;
            ApplicationLifetime = applicationLifetime;
            ZwiftTelemetry = zwiftTelemetry;
            ZwiftPacketMonitor = zwiftPacketMonitor;
        }

        private ILogger<ZwiftMonitorService> Logger {get;}
        private IHostApplicationLifetime ApplicationLifetime {get;}
        private ZwiftTelemetry ZwiftTelemetry {get;}
        private ZwiftPacketMonitor.Monitor ZwiftPacketMonitor {get;}

        public Task StartAsync(CancellationToken cancellationToken) {
            ApplicationLifetime.ApplicationStarted.Register(OnStarted);
            ApplicationLifetime.ApplicationStopping.Register(OnStopping);
            ApplicationLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        private void OnStarted() {
            Logger.LogInformation("OnStarted has been called.");

            ZwiftPacketMonitor.IncomingPlayerEvent += (s, e) => {
                //Logger.LogInformation($"INCOMING: {e.PlayerState}");
                //ZwiftTelemetry.UpdatePlayerState(e.PlayerState);
            };

            ZwiftPacketMonitor.OutgoingPlayerEvent += (s, e) => {
                //Logger.LogInformation($"OUTGOING: {e.PlayerState}");
                ZwiftTelemetry.UpdatePlayerState(e.PlayerState);
            };

            Task.Run(() => 
            {
                try 
                {
                    Logger.LogInformation("StartCaptureAsync");
                    ZwiftPacketMonitor.StartCaptureAsync("en0").Wait();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "ZwiftPacketMonitor.StartCaptureAsync");
                }
            });
        }

        private void OnStopping() {
            Logger.LogInformation("OnStopping has been called.");
            ZwiftPacketMonitor.StopCaptureAsync().Wait();
        }

        private void OnStopped() {
            Logger.LogInformation("OnStopped has been called.");
        }
    }
}