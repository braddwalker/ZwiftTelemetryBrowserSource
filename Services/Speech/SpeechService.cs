using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.CognitiveServices.Speech;
using System.Net;

namespace ZwiftTelemetryBrowserSource.Services.Speech
{
    public class SpeechService
    {
        private const string SSML_TEMPLATE = "ssml_template.xml";
        private const double SPEECH_SPEED = 1.20;
        private readonly ILogger<SpeechService> Logger;
        private readonly SpeechOptions Options;
        private readonly string SubscriptionKey;

        private readonly string ssmlTemplate;

        public SpeechService(ILogger<SpeechService> logger, IOptions<SpeechOptions> speechOptions)
        {
            Logger = logger;
            Options = speechOptions.Value;

            if (Options.Enabled) 
            {
                Logger.LogInformation("Speech service enabled");
                SubscriptionKey = File.ReadAllText(Options.SubscriptionKeyFile).Trim();
                Logger.LogInformation($"Azure key loaded from {new FileInfo(Options.SubscriptionKeyFile).FullName}");

                ssmlTemplate = File.ReadAllText(SSML_TEMPLATE);
                Logger.LogInformation($"SSML template loaded from {new FileInfo(SSML_TEMPLATE).FullName}");
            }
        }

        public async Task<string> GetAudioBase64(string message, string voiceName)
        {
            string audioBase64 = "";

            try 
            {
                if (Options.Enabled)
                {
                    Logger.LogDebug($"Voice: {voiceName}, Message: {message}");

                    var config = SpeechConfig.FromSubscription(SubscriptionKey, Options.Region);
                    config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);
                    config.SpeechSynthesisVoiceName = voiceName;
                    
                    byte[] buffer = new byte[10240];
                    List<byte> b = new List<byte>();

                    using (var synthesizer = new SpeechSynthesizer(config, null))
                    {
                        var ssml = ssmlTemplate
                            .Replace("{voiceName}", voiceName)
                            .Replace("{speed}", $"{SPEECH_SPEED:0.00}")
                            .Replace("{message}", WebUtility.HtmlEncode(message));

                        Logger.LogDebug($"SSML: {ssml}");

                        var result = await synthesizer.SpeakSsmlAsync(ssml);
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
                            Logger.LogWarning($"Unable to synthesize speech - {result.Reason} - {result.ResultId}");
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