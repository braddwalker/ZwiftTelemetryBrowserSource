using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZwiftTelemetryBrowserSource.Util;
using ZwiftTelemetryBrowserSource.Services.Alerts;
using ZwiftTelemetryBrowserSource.Services.Notifications;
using ZwiftTelemetryBrowserSource.Models;
using Newtonsoft.Json;
using System.Globalization;

namespace ZwiftTelemetryBrowserSource.Services.Speech
{
    /// <summary>
    /// This is a Zwift-specific implementation on top of the <c>SpeechService</c> layer in that it maintains a mapping of
    /// riderId values and randomly selected voice names. The end result is that each rider gets the same voice anytime
    /// they speek. This mapping is transient, and is reset anytime the program reloads, or an explicit call to ResetVoices
    /// is made.
    /// </summary>
    public class ZwiftTTSService : BaseZwiftService
    {
        private readonly ILogger<ZwiftTTSService> _logger;
        private readonly SpeechService _speechService;
        private readonly SpeechOptions _speechConfig;
        private readonly ZwiftMonitorService _zwiftService;
        private readonly AlertsConfig _alertsConfig;
        private readonly IChatNotificationsService _chatNotificationsService;

        private IDictionary<int, string> _riderVoices;
        private EventService _eventService;
        private int _currentGroupId;
        private int _currentRiderId;

        public ZwiftTTSService(ILogger<ZwiftTTSService> logger, SpeechService speechService, IOptions<SpeechOptions> speechConfig, 
            EventService eventService, ZwiftMonitorService zwiftService, IOptions<AlertsConfig> alertsConfig, 
            IChatNotificationsService chatNotificationsService) : base(logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _speechService = speechService ?? throw new ArgumentException(nameof(speechService));
            _speechConfig = speechConfig?.Value ?? throw new ArgumentException(nameof(speechConfig));
            _eventService = eventService ?? throw new ArgumentException(nameof(eventService));
            _zwiftService = zwiftService ?? throw new ArgumentException(nameof(zwiftService));
            _alertsConfig = alertsConfig?.Value ?? throw new ArgumentException(nameof(alertsConfig));
            _chatNotificationsService = chatNotificationsService ?? throw new ArgumentException(nameof(chatNotificationsService));
            _riderVoices = new Dictionary<int, string>();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _eventService.EventChanged += (s, e) =>
            {
                ResetVoices();
            };

            _zwiftService.OutgoingPlayerEvent += (s, e) => {
                _currentRiderId = e.PlayerState.Id;
                _currentGroupId = e.PlayerState.GroupId;
            };

            _zwiftService.IncomingChatMessageEvent += async (s, e) =>
            {
                if (_alertsConfig.Chat.Enabled)
                {
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
                                AudioSource = await GetAudioBase64(e.Message.RiderId, e.Message.Message, countryCode),
                                Avatar = GetRiderProfileImage(e.Message.Avatar),
                                CountryCode = countryCode
                            });

                            await _chatNotificationsService.SendNotificationAsync(message);
                        }
                    }
                }
            };

            await Task.CompletedTask;
        }

        private string GetRiderProfileImage(string avatarUrl)
        {
            if (_alertsConfig.Chat.ShowProfileImage)
            {
                return (string.IsNullOrWhiteSpace(avatarUrl) ? "/images/avatar.jpg" : avatarUrl);
            }
            else 
            {
                return (string.Empty);
            }
        }

        /// <summary>
        /// Manually resets all voice assignments.
        /// </summary>
        private void ResetVoices()
        {
            if (!_speechConfig.Enabled)
                return;

            _logger.LogDebug("Resetting voice assignments");
            _riderVoices = new Dictionary<int, string>();
        }

        /// <summary>
        /// Delegates to the underlying <c>SpeechService</c> for actual TTS, but first will try to find an existing
        /// RiderId/VoiceName assignment. If none found, a new one is created by randomly choosing one of the available
        /// voices for the given country.
        /// </summary>
        /// <param name="riderId">The riderId performing the action</param>
        /// <param name="message">The message being spoken</param>
        /// <param name="countryCode">The rider's country of origin</param>
        /// <returns>A Base64 encoded audio stream</returns>
        private async Task<string> GetAudioBase64(int riderId, string message, string countryCode)
        {
            if (!_speechConfig.Enabled)
                return (null);

            var voiceName = _speechConfig.DefaultVoiceName;

            // See if we've already assigned a voice to this rider
            if (_riderVoices.ContainsKey(riderId))
            {
                voiceName = _riderVoices[riderId];
            }
            else
            {
                // See if we can select a voice from the list of configured ones based on the country code.
                // If no match is found, fall back on the default voice
                var random = new Random();
                
                var voices = _speechConfig.Voices?.Where(x => x.Country == countryCode).ToList();
                if (voices?.Count > 0)
                {
                    voiceName = voices.ElementAt(random.Next(voices.Count)).VoiceName;
                    _riderVoices[riderId] = voiceName;
                }
            }

            _logger.LogDebug($"Rider: {riderId}, Country: {countryCode}, Voice: {voiceName}, Message: {message}");
            return (await _speechService.GetAudioBase64(message, voiceName));
        }
    }
}