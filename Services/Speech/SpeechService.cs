using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.CognitiveServices.Speech;

namespace ZwiftTelemetryBrowserSource.Services.Speech
{
    public class SpeechService
    {
        private readonly ILogger<SpeechService> Logger;
        private readonly SpeechOptions Options;

        public SpeechService(ILogger<SpeechService> logger, IOptions<SpeechOptions> speechOptions)
        {
            Logger = logger;
            Options = speechOptions.Value;
        }

        public async Task<string> TTSAudioBase64(string message)
        {
            var config = SpeechConfig.FromSubscription(Options.SubscriptionKey, Options.Region);
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);
            config.SpeechSynthesisVoiceName = Options.VoiceName;
            
            byte[] buffer = new byte[10240];
            List<byte> b = new List<byte>();

            using (var synthesizer = new SpeechSynthesizer(config, null))
            {
                var result = await synthesizer.SpeakTextAsync(message);
                using (var stream = AudioDataStream.FromResult(result))
                {
                    int bytesRead = (int)stream.ReadData(buffer);
                    while (bytesRead > 0)
                    {
                        b.AddRange(buffer.Take(bytesRead));

                        buffer = new byte[10240];
                        bytesRead = (int)stream.ReadData(buffer);
                    }
                }
            }

            return ($"data:audio/x-mp3;base64,{Convert.ToBase64String(b.ToArray())}");
        }
    }
}