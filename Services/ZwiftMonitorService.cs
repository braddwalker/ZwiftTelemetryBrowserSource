using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Globalization;
using ZwiftTelemetryBrowserSource.Util;
using ZwiftTelemetryBrowserSource.Models;
using ZwiftTelemetryBrowserSource.Services.Notifications;
using ZwiftTelemetryBrowserSource.Services.Speech;
using ZwiftTelemetryBrowserSource.Services.Alerts;
using ZwiftTelemetryBrowserSource.Services.Twitch;
using ZwiftTelemetryBrowserSource.Services.Results;
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
            ZwiftTTSService zwiftTTSService,
            IOptions<AlertsConfig> alertsConfig,
            TwitchIrcService twitchIrcService,
            ResultsService resultsService) 
        {

            _config = config ?? throw new ArgumentException(nameof(config));
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _zwiftPacketMonitor = zwiftPacketMonitor ?? throw new ArgumentException(nameof(zwiftPacketMonitor));
            _telemetryNotificationsService = telemetryNotificationsService ?? throw new ArgumentException(nameof(telemetryNotificationsService));
            _chatNotificationsService = chatNotificationsService ?? throw new ArgumentException(nameof(chatNotificationsService));
            _rideOnNotificationService = rideOnNotificationService ?? throw new ArgumentException(nameof(rideOnNotificationService));
            _averageTelemetryService = averageTelemetryService ?? throw new ArgumentException(nameof(averageTelemetryService));
            _speechService = zwiftTTSService ?? throw new ArgumentException(nameof(zwiftTTSService));
            _alertsConfig = alertsConfig?.Value ?? throw new ArgumentException(nameof(alertsConfig));
            _twitchIrcService = twitchIrcService ?? throw new ArgumentException(nameof(twitchIrcService));
            _resultsService = resultsService ?? throw new ArgumentException(nameof(resultsService));
        }

        private ITelemetryNotificationsService _telemetryNotificationsService {get;}
        private IChatNotificationsService _chatNotificationsService {get;}
        private IRideOnNotificationService _rideOnNotificationService {get;}
        private IConfiguration _config {get;}
        private ILogger<ZwiftMonitorService> _logger {get;}
        private ZwiftPacketMonitor.Monitor _zwiftPacketMonitor {get;}
        private AverageTelemetryService _averageTelemetryService {get;}
        private ZwiftTTSService _speechService {get;}
        private AlertsConfig _alertsConfig {get;}
        private TwitchIrcService _twitchIrcService {get;}
        private ResultsService _resultsService {get;}
        
        // The Zwift ID of the player being tracked. This is either
        // YOU the actual rider, or another rider chosen at random for debug mode
        private int _currentRiderId;
        
        // This is used to track when a player enters/leaves an event
        private int _currentGroupId;

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping ZwiftMonitorService");
            await _zwiftPacketMonitor.StopCaptureAsync();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting ZwiftMonitorService");

            _zwiftPacketMonitor.OutgoingPlayerEvent += (s, e) => {
                try 
                {
                    _logger.LogDebug(e.PlayerState.ToString());
                    _resultsService.RegisterResults(e.PlayerState);

                    // Need to hang on to this for later
                    _currentRiderId = e.PlayerState.Id;

                    DispatchPlayerStateUpdate(e.PlayerState);

                    // See if we need to trigger an event/world changed
                    if (_currentGroupId != e.PlayerState.GroupId)
                    {
                        _logger.LogDebug($"World/event change detected from {_currentGroupId} to {e.PlayerState.GroupId}");
                        _currentGroupId = e.PlayerState.GroupId;

                        // Reset the speech service voices since we're in a new world
                        _speechService.ResetVoices();

                        // Just to be explicit, we should reset anytime the world changes
                        _resultsService.Reset(e.PlayerState.GroupId);

                        // If we are entering an event, aways reset average telemetry
                        // If we are leaving an event, let the config decide
                        if ((e.PlayerState.GroupId != 0) 
                            || (e.PlayerState.GroupId == 0 && _config.GetValue<bool>("ResetAveragesOnEventFinish")))
                        {
                            _averageTelemetryService.Reset();   
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "OutgoingPlayerEvent");
                }
            };

            _zwiftPacketMonitor.IncomingPlayerEvent += (s, e) => {
                // _logger.LogDebug(e.PlayerState.ToString());
                _resultsService.RegisterResults(e.PlayerState);
            };

            _zwiftPacketMonitor.IncomingChatMessageEvent += async (s, e) => {
                _logger.LogInformation($"CHAT: {e.Message.ToString()}, {RegionInfo.CurrentRegion.IsoCodeFromNumeric(e.Message.CountryCode)}");

                // Only alert chat messages that are actually visible to the player in the game
                if (_currentGroupId == e.Message.EventSubgroup)
                {
                    // See if we're configured to read own messages if this came from us
                    if (_alertsConfig.Chat.AlertOwnMessages || (e.Message.RiderId != _currentRiderId))
                    {
                        var countryCode = RegionInfo.CurrentRegion.IsoCodeFromNumeric(e.Message.CountryCode);
                        var message = JsonConvert.SerializeObject(new ChatNotificationModel()
                        {
                            RiderId = e.Message.RiderId,
                            FirstName = e.Message.FirstName,
                            LastName = e.Message.LastName,
                            Message = e.Message.Message,
                            AudioSource = await _speechService.GetAudioBase64(e.Message.RiderId, e.Message.Message, countryCode),
                            Avatar = GetRiderProfileImage(e.Message.Avatar),
                            CountryCode = countryCode
                        });

                        _chatNotificationsService.SendNotificationAsync(message).Wait();
                    }
                }
            };

            _zwiftPacketMonitor.IncomingPlayerEnteredWorldEvent += (s, e) => {
                //_logger.LogInformation($"WORLD: {e.PlayerUpdate.ToString()}");
                _resultsService.RegisterRider(e.PlayerUpdate.F2, $"{e.PlayerUpdate.FirstName} {e.PlayerUpdate.LastName}");
            };

            _zwiftPacketMonitor.IncomingRideOnGivenEvent += (s, e) => {
                _logger.LogInformation($"RIDEON: {e.RideOn.ToString()}");

                var message = JsonConvert.SerializeObject(new RideOnNotificationModel()
                {
                    RiderId = e.RideOn.RiderId,
                    FirstName = e.RideOn.FirstName,
                    LastName = e.RideOn.LastName,
                    AudioSource = "/audio/rockon.ogg"
                });

                _rideOnNotificationService.SendNotificationAsync(message).Wait();

                _twitchIrcService.SendPublicChatMessage($"Thanks for the ride on, {e.RideOn.FirstName} {e.RideOn.LastName}!");
            };

            await _zwiftPacketMonitor.StartCaptureAsync(_config.GetValue<string>("NetworkInterface"), cancellationToken);
        }

        private string GetRiderProfileImage(string avatarUrl)
        {
            if (_alertsConfig.Chat.ShowProfileImage)
            {
                return (string.IsNullOrWhiteSpace(avatarUrl) ? "/images/avatar.jpg" : avatarUrl);
            }
            else 
            {
                return ("");
            }
        }

        private void DispatchPlayerStateUpdate(ZwiftPacketMonitor.PlayerState state) {
            var summary = _averageTelemetryService.LogTelemetry(state);

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

            _telemetryNotificationsService.SendNotificationAsync(telemetry).Wait();
        }
    }
}