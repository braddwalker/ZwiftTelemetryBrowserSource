using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZwiftTelemetryBrowserSource.Services.Speech
{
    /// <summary>
    /// This is a Zwift-specific implementation on top of the <c>SpeechService</c> layer in that it maintains a mapping of
    /// riderId values and randomly selected voice names. The end result is that each rider gets the same voice anytime
    /// they speek. This mapping is transient, and is reset anytime the program reloads, or an explicit call to ResetVoices
    /// is made.
    /// </summary>
    public class ZwiftTTSService
    {
        private ILogger<ZwiftTTSService> _logger;
        private SpeechService _speechService;
        private SpeechOptions _speechConfig;
        private IDictionary<int, string> _riderVoices;

        public ZwiftTTSService(ILogger<ZwiftTTSService> logger, SpeechService speechService, IOptions<SpeechOptions> speechConfig)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _speechService = speechService ?? throw new ArgumentException(nameof(speechService));
            _speechConfig = speechConfig?.Value ?? throw new ArgumentException(nameof(speechConfig));
            _riderVoices = new Dictionary<int, string>();
        }

        /// <summary>
        /// Manually resets all voice assignments.
        /// </summary>
        public void ResetVoices()
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
        public async Task<string> GetAudioBase64(int riderId, string message, string countryCode)
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