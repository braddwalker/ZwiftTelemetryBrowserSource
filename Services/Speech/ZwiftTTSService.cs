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
        private ILogger<ZwiftTTSService> logger;
        private SpeechService speechService;
        private SpeechOptions speechConfig;
        private IDictionary<int, string> riderVoices;

        public ZwiftTTSService(ILogger<ZwiftTTSService> logger, SpeechService speechService, IOptions<SpeechOptions> speechConfig)
        {
            this.logger = logger;
            this.speechService = speechService;
            this.speechConfig = speechConfig.Value;
            riderVoices = new Dictionary<int, string>();
        }

        /// <summary>
        /// Manually resets all voice assignments.
        /// </summary>
        public void ResetVoices()
        {
            logger.LogDebug("Resetting voice assignments");
            riderVoices = new Dictionary<int, string>();
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
            var voiceName = speechConfig.DefaultVoiceName;

            // See if we've already assigned a voice to this rider
            if (riderVoices.ContainsKey(riderId))
            {
                voiceName = riderVoices[riderId];
            }
            else
            {
                // See if we can select a voice from the list of configured ones based on the country code.
                // If no match is found, fall back on the default voice
                var random = new Random();
                
                var voices = speechConfig.Voices?.Where(x => x.Country == countryCode).ToList();
                if (voices?.Count > 0)
                {
                    voiceName = voices.ElementAt(random.Next(voices.Count)).VoiceName;
                    riderVoices[riderId] = voiceName;
                }
            }

            logger.LogDebug($"Rider: {riderId}, Country: {countryCode}, Voice: {voiceName}, Message: {message}");
            return (await speechService.GetAudioBase64(message, voiceName));
        }
    }
}