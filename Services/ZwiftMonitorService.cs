using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using System;
using ZwiftTelemetryBrowserSource.Models;
using ZwiftTelemetryBrowserSource.Services.Notifications;
using ZwiftTelemetryBrowserSource.Services.Speech;
using ZwiftTelemetryBrowserSource.Services.Alerts;
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
            ITelemetryNotificationsService telemetryNotificationsService,
            IChatNotificationsService chatNotificationsService,
            IRideOnNotificationService rideOnNotificationService,
            AverageTelemetryService averageTelemetryService,
            SpeechService speechService,
            AlertsService alertsService,
            IOptions<AlertsConfig> alertsConfig) {

            Config = config;
            Logger = logger;
            ZwiftPacketMonitor = zwiftPacketMonitor;
            TelemetryNotificationsService = telemetryNotificationsService;
            ChatNotificationsService = chatNotificationsService;
            RideOnNotificationService = rideOnNotificationService;
            AverageTelemetryService = averageTelemetryService;
            SpeechService = speechService;
            AlertsService = alertsService;
            AlertsConfig = alertsConfig.Value;
        }

        private ITelemetryNotificationsService TelemetryNotificationsService {get;}
        private IChatNotificationsService ChatNotificationsService {get;}
        private IRideOnNotificationService RideOnNotificationService {get;}
        private IConfiguration Config {get;}
        private ILogger<ZwiftMonitorService> Logger {get;}
        private ZwiftPacketMonitor.Monitor ZwiftPacketMonitor {get;}
        private AverageTelemetryService AverageTelemetryService {get;}
        private SpeechService SpeechService {get;}
        private AlertsService AlertsService {get;}
        private AlertsConfig AlertsConfig {get;}
        
        // The Zwift ID of the player being tracked. This is either
        // YOU the actual rider, or another rider chosen at random for debug mode
        private int currentRiderId;
        
        // This is used to track when a player enters/leaves an event
        private int currentGroupId;

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping ZwiftMonitorService");
            await ZwiftPacketMonitor.StopCaptureAsync();
        }

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
                        if ((currentRiderId == 0) || (currentRiderId == e.PlayerState.Id))
                        {
                            currentRiderId = e.PlayerState.Id;
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
                // packets, which represent YOU, that are being sent to the Zwift servers.
                ZwiftPacketMonitor.OutgoingPlayerEvent += (s, e) => {
                    try 
                    {
                        // Need to hang on to this for later
                        currentRiderId = e.PlayerState.Id;

                        DispatchPlayerStateUpdate(e.PlayerState);

                        // See if we need to trigger an event/world changed
                        if (currentGroupId != e.PlayerState.GroupId)
                        {
                            Logger.LogDebug($"World/event change detected from {currentGroupId} to {e.PlayerState.GroupId}");
                            currentGroupId = e.PlayerState.GroupId;

                            // If we are entering an event, aways reset average telemetry
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
                        // See if we're configured to read own messages if this came from us
                        if (AlertsConfig.Chat.AlertOwnMessages || (e.Message.RiderId != currentRiderId))
                        {
                            Logger.LogInformation($"CHAT: {e.Message.ToString()}");

                            var message = JsonConvert.SerializeObject(new ChatNotificationModel()
                            {
                                RiderId = e.Message.RiderId,
                                FirstName = e.Message.FirstName,
                                LastName = e.Message.LastName,
                                Message = e.Message.Message,
                                AudioSource = await SpeechService.GetAudioBase64(e.Message.Message),
                                Avatar = e.Message.Avatar
                            });

                            ChatNotificationsService.SendNotificationAsync(message).Wait();
                        }
                    }
                };

                ZwiftPacketMonitor.IncomingPlayerEnteredWorldEvent += (s, e) => {
                    //Logger.LogInformation($"WORLD: {e.PlayerUpdate.ToString()}");
                };

                ZwiftPacketMonitor.IncomingRideOnGivenEvent += (s, e) => {
                    Logger.LogInformation($"RIDEON: {e.RideOn.ToString()}");

                    var message = JsonConvert.SerializeObject(new RideOnNotificationModel()
                    {
                        RiderId = e.RideOn.RiderId,
                        FirstName = e.RideOn.FirstName,
                        LastName = e.RideOn.LastName,
                        AudioSource = "/audio/rockon.ogg"
                    });

                    RideOnNotificationService.SendNotificationAsync(message).Wait();
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

            TelemetryNotificationsService.SendNotificationAsync(telemetry).Wait();
        }
    }
}