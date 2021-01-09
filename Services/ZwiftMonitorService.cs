using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System;
using ZwiftTelemetryBrowserSource.Models;
using Newtonsoft.Json;

namespace ZwiftTelemetryBrowserSource.Services
{
    /// <summary>
    /// This service is responsible for listening for Zwift game events, dispatched by the ZwiftPacketMonitor
    /// library. When game events are received, they then get dispatched to the client webpage via a Server-Side
    /// Events implementation.
    /// </summary>
    public class ZwiftMonitorService : BackgroundService
    {
        public ZwiftMonitorService(ILogger<ZwiftMonitorService> logger, 
            IHostApplicationLifetime applicationLifetime,
            ZwiftPacketMonitor.Monitor zwiftPacketMonitor,
            IConfiguration config,
            INotificationsService notificationsService) {

            Config = config;
            Logger = logger;
            ApplicationLifetime = applicationLifetime;
            ZwiftPacketMonitor = zwiftPacketMonitor;
            NotificationsService = notificationsService;
        }

        private INotificationsService NotificationsService {get;}
        private IConfiguration Config {get;}
        private ILogger<ZwiftMonitorService> Logger {get;}
        private IHostApplicationLifetime ApplicationLifetime {get;}
        private ZwiftPacketMonitor.Monitor ZwiftPacketMonitor {get;}
        private int trackedPlayerId;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Starting ZwiftMonitorService");

            // Debug mode will operate a little differently than the regular game mode.
            // When debug mode is on, we'll pick the first INCOMING player's data to use, and will lock
            // onto that PlayerId to filter out subsequent updates. This makes the testing more consistent. 
            // This way it's possible to test event dispatch w/o having to be on the bike with power meter 
            // and heart rate strap actually connected and outputting data.
            if (Config.GetValue<bool>("Debug"))
            {
                Logger.LogInformation("Debug mode enabled");

                ZwiftPacketMonitor.IncomingPlayerEvent += (s, e) => {     
                    try 
                    {               
                        if ((trackedPlayerId == 0) || (trackedPlayerId == e.PlayerState.Id))
                        {
                            trackedPlayerId = e.PlayerState.Id;
                            DispatchPlayerStateUpdate(e.PlayerState);
                        }
                    }
                    catch (Exception ex) {
                        Logger.LogError(ex, "IncomingPlayerEvent");
                    }
                };
            }
            else {
                // Under normal circumstances we will only be dispatching telemetry data from OUTGOING
                // packets, which represent YOU, that are being sent out to the Zwift servers.
                ZwiftPacketMonitor.OutgoingPlayerEvent += (s, e) => {
                    try 
                    {
                        DispatchPlayerStateUpdate(e.PlayerState);
                    }
                    catch (Exception ex) {
                        Logger.LogError(ex, "OutgoingPlayerEvent");
                    }
                };
            }

            await ZwiftPacketMonitor.StartCaptureAsync(Config.GetValue<string>("NetworkInterface"), cancellationToken);
        }

        private void DispatchPlayerStateUpdate(ZwiftPacketMonitor.PlayerState state) {
            var telemetry = JsonConvert.SerializeObject(new TelemetryModel()
            {
                PlayerId = state.Id,
                Power = state.Power,
                HeartRate = state.Heartrate
            });

            NotificationsService.SendNotificationAsync(telemetry, false).Wait();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping ZwiftMonitorService");
            await ZwiftPacketMonitor.StopCaptureAsync();
        }
    }
}