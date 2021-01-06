using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
            ZwiftPacketMonitor.Monitor zwiftPacketMonitor,
            IConfiguration config) {

            Config = config;
            Logger = logger;
            ApplicationLifetime = applicationLifetime;
            ZwiftTelemetry = zwiftTelemetry;
            ZwiftPacketMonitor = zwiftPacketMonitor;
        }

        private IConfiguration Config {get;}
        private ILogger<ZwiftMonitorService> Logger {get;}
        private IHostApplicationLifetime ApplicationLifetime {get;}
        private ZwiftTelemetry ZwiftTelemetry {get;}
        private ZwiftPacketMonitor.Monitor ZwiftPacketMonitor {get;}

        private ZwiftPacketMonitor.PlayerState playerState = null;

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

            // For debug testing we can simply browse other riders and
            // "borrow" their telemetry
            if (Config.GetValue<bool>("Debug"))
            {
                Logger.LogInformation("Debug mode enabled");

                ZwiftPacketMonitor.IncomingPlayerEvent += (s, e) => {                    
                    // We'll take whoever is the first player update to come in and
                    // will ignore others (makes the testing more consistent)
                    if ((playerState == null) || (playerState.Id == e.PlayerState.Id))
                    {
                        playerState = e.PlayerState;
                        ZwiftTelemetry.UpdatePlayerState(e.PlayerState);
                    }
                };
            }
            else {
                ZwiftPacketMonitor.OutgoingPlayerEvent += (s, e) => {
                    ZwiftTelemetry.UpdatePlayerState(e.PlayerState);
                };
            }

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