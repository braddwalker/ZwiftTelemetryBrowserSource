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

        private readonly ILogger<SpeechService> _logger;
        private readonly SpeechOptions _options;
        private readonly string _subscriptionKey;
        private readonly string _ssmlTemplate;

        public SpeechService(ILogger<SpeechService> logger, IOptions<SpeechOptions> options)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentException(nameof(options));

            if (_options.Enabled) 
            {
                if (File.Exists(_options.SubscriptionKeyFile))
                {
                    _subscriptionKey = File.ReadAllText(_options.SubscriptionKeyFile).Trim();
                    _logger.LogInformation("Speech service enabled");
                    _logger.LogInformation($"Azure key loaded from {new FileInfo(_options.SubscriptionKeyFile).FullName}");
                
                    _ssmlTemplate = File.ReadAllText(SSML_TEMPLATE);
                    _logger.LogInformation($"SSML template loaded from {new FileInfo(SSML_TEMPLATE).FullName}");
                }
                else
                {
                    _logger.LogWarning($"Unable to find key file {new FileInfo(_options.SubscriptionKeyFile).FullName}");
                    _options.Enabled = false;
                }
            }
        }

        public async Task<string> GetAudioBase64(string message, string voiceName)
        {
            string audioBase64 = "";

            try 
            {
                if (_options.Enabled)
                {
                    var config = SpeechConfig.FromSubscription(_subscriptionKey, _options.Region);
                    config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);
                    config.SpeechSynthesisVoiceName = voiceName;
                    
                    byte[] buffer = new byte[10240];
                    List<byte> b = new List<byte>();

                    using (var synthesizer = new SpeechSynthesizer(config, null))
                    {
                        var ssml = _ssmlTemplate
                            .Replace("{voiceName}", voiceName)
                            .Replace("{speed}", $"{SPEECH_SPEED:0.00}")
                            .Replace("{message}", WebUtility.HtmlEncode(message));

                        _logger.LogDebug($"SSML: {ssml}");

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
                                    _logger.LogDebug($"StreamStatus returned {stream.GetStatus()}");
                                }
                            }
                        }
                        else 
                        {
                            _logger.LogWarning($"Unable to synthesize speech - {result.Reason} - {result.ResultId}");
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "GetAudioBase64");
                return ("");
            }

            return (audioBase64);
        }
    }
}