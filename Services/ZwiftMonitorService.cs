using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System;
using ZwiftTelemetryBrowserSource.Models;
using ZwiftTelemetryBrowserSource.Services.Notifications;
using ZwiftTelemetryBrowserSource.Services.Speech;
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
            ZwiftPacketMonitor.Monitor zwiftPacketMonitor,
            IConfiguration config,
            INotificationsService notificationsService,
            IRideOnNotificationService rideOnNotificationService,
            AverageTelemetryService averageTelemetryService,
            SpeechService speechService) {

            Config = config;
            Logger = logger;
            ZwiftPacketMonitor = zwiftPacketMonitor;
            NotificationsService = notificationsService;
            RideOnNotificationService = rideOnNotificationService;
            AverageTelemetryService = averageTelemetryService;
            SpeechService = speechService;
        }

        private INotificationsService NotificationsService {get;}
        private IRideOnNotificationService RideOnNotificationService {get;}
        private IConfiguration Config {get;}
        private ILogger<ZwiftMonitorService> Logger {get;}
        private ZwiftPacketMonitor.Monitor ZwiftPacketMonitor {get;}
        private AverageTelemetryService AverageTelemetryService {get;}
        private SpeechService SpeechService {get;}
        
        // For debugging purposes only
        private int trackedPlayerId;
        
        // This is used to track when a player enters/leaves an event
        private int currentGroupId;

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

                        // See if we need to trigger an event/world changed
                        if (currentGroupId != e.PlayerState.GroupId)
                        {
                            Logger.LogDebug($"World/event change detected from {currentGroupId} to {e.PlayerState.GroupId}");
                            currentGroupId = e.PlayerState.GroupId;

                            // If we are entering an event, aways reset
                            // If we are leaving an event, let the config decide
                            if ((e.PlayerState.GroupId != 0) 
                                || (e.PlayerState.GroupId == 0 && Config.GetValue<bool>("ResetAveragesOnEventFinish")))
                            {
                                AverageTelemetryService.Reset();   
                            }
                        }
                    }
                    catch (Exception ex) {
                        Logger.LogError(ex, "OutgoingPlayerEvent");
                    }
                };

                ZwiftPacketMonitor.IncomingChatMessageEvent += async (s, e) => {
                    // Only process chat messages when they come from the same group we're currently in
                    if (e.Message.EventSubgroup == currentGroupId)
                    {
                        Logger.LogInformation($"CHAT: {e.Message.ToString()}");

                        var message = JsonConvert.SerializeObject(new RideOnNotificationModel()
                        {
                            PlayerId = e.Message.RiderId,
                            FirstName = e.Message.FirstName,
                            LastName = e.Message.LastName,
                            Message = e.Message.Message,
                            AudioSource = await SpeechService.GetAudioBase64(e.Message.Message),
                            Avatar = e.Message.Avatar
                        });

                        RideOnNotificationService.SendNotificationAsync(message, false).Wait();
                    }
                };

                ZwiftPacketMonitor.IncomingPlayerEnteredWorldEvent += (s, e) => {
                    //Logger.LogInformation($"WORLD: {e.PlayerUpdate.ToString()}");
                };

                ZwiftPacketMonitor.IncomingRideOnGivenEvent += (s, e) => {
                    Logger.LogInformation($"RIDEON: {e.RideOn.ToString()}");

                    var message = JsonConvert.SerializeObject(new RideOnNotificationModel()
                    {
                        PlayerId = e.RideOn.RiderId,
                        FirstName = e.RideOn.FirstName,
                        LastName = e.RideOn.LastName,
                        Message = $"{e.RideOn.FirstName} {e.RideOn.LastName} gave you a ride on!",
                        AudioSource = "/audio/rockon.ogg"
                    });

                    RideOnNotificationService.SendNotificationAsync(message, false).Wait();
                };
            }

            await ZwiftPacketMonitor.StartCaptureAsync(Config.GetValue<string>("NetworkInterface"), cancellationToken);
        }

        private void DispatchPlayerStateUpdate(ZwiftPacketMonitor.PlayerState state) {
            var summary = AverageTelemetryService.LogTelemetry(state);

            var telemetry = JsonConvert.SerializeObject(new TelemetryModel()
            {
                PlayerId = state.Id,
                Power = state.Power,
                HeartRate = state.Heartrate,
                AvgPower = summary.Power,
                AvgHeartRate = summary.Heartrate,
                AvgSpeed = summary.Speed,
                AvgCadence = summary.Cadence
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