using System;
using System.Linq;
using System.IO;
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
        private readonly string SubscriptionKey;

        public SpeechService(ILogger<SpeechService> logger, IOptions<SpeechOptions> speechOptions)
        {
            Logger = logger;
            Options = speechOptions.Value;

            if (Options.Enabled) 
            {
                logger.LogInformation("Speech service enabled");

                var file = Path.Combine(Directory.GetCurrentDirectory(), Options.SubscriptionKeyFile);
                SubscriptionKey = File.ReadAllText(file).Trim();
                logger.LogInformation($"Azure key loaded from {file}");
            }
        }

        public async Task<string> GetAudioBase64(string message)
        {
            string audioBase64 = "";

            try 
            {
                if (Options.Enabled)
                {
                    var config = SpeechConfig.FromSubscription(SubscriptionKey, Options.Region);
                    config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);
                    config.SpeechSynthesisVoiceName = Options.VoiceName;
                    
                    byte[] buffer = new byte[10240];
                    List<byte> b = new List<byte>();

                    using (var synthesizer = new SpeechSynthesizer(config, null))
                    {
                        var result = await synthesizer.SpeakTextAsync(message);
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            using (var stream = AudioDataStream.FromResult(result))
                            {
                                if (stream.GetStatus() == StreamStatus.AllData)
                                {
                                    int bytesRead = (int)stream.ReadData(buffer);
                                    while (bytesRead > 0)
                                    {
                                        b.AddRange(buffer.Take(bytesRead));

                                        buffer = new byte[10240];
                                        bytesRead = (int)stream.ReadData(buffer);
                                    }

                                    audioBase64 = $"data:audio/x-mp3;base64,{Convert.ToBase64String(b.ToArray())}";
                                }
                                else 
                                {
                                    Logger.LogDebug($"StreamStatus returned {stream.GetStatus()}");
                                }
                            }
                        }
                        else 
                        {
                            Logger.LogWarning($"Unable to synthesize speech for: {message}");
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                Logger.LogError(ex, "GetAudioBase64");
                return ("");
            }

            return (audioBase64);
        }
    }
}