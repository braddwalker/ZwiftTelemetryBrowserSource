using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using IrcDotNet;

namespace ZwiftTelemetryBrowserSource.Services.Twitch
{
    public class TwitchIrcService : BackgroundService
    {
        private const int WAIT_TIMEOUT = 10000;

        private IrcDotNet.TwitchIrcClient _ircClient;
        private TwitchConfig _twitchConfig;
        private string _twitchOAuthKey;
        private ILogger<TwitchIrcService> _logger;

        private ManualResetEventSlim _connectedEvent;
        private ManualResetEventSlim _registeredEvent;
        private bool _shutdown;

        private ConcurrentQueue<string> _messageQueue;
        

        public TwitchIrcService(ILogger<TwitchIrcService> logger, IOptions<TwitchConfig> twitchConfig)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _twitchConfig = twitchConfig?.Value ?? throw new ArgumentException(nameof(twitchConfig));
            _shutdown = false;
            _messageQueue = new ConcurrentQueue<string>();

            if (_twitchConfig.Enabled)
            {
                _logger.LogInformation("TwitchIrcService enabled");

                _twitchOAuthKey = File.ReadAllText(_twitchConfig.AuthTokenFile).Trim();
                _logger.LogInformation($"Twitch OAuth key loaded from {new FileInfo(_twitchConfig.AuthTokenFile).FullName}");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_twitchConfig.Enabled)
                return;

            _logger.LogInformation("Starting TwitchIrcService");

            _ircClient = new IrcDotNet.TwitchIrcClient();
            _ircClient.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            _ircClient.Disconnected += (s,e) => {
                _logger.LogDebug("Twitch IRC disconnected");

                // Attempt to reconnect so long as we're not shutting down
                if (!_shutdown)
                {
                    _logger.LogDebug("Twitch IRC attempting to reconnect");
                    ConnectIrc(cancellationToken);
                }
            };
            _ircClient.Connected += (s,e) => {
                _logger.LogDebug("Twitch IRC connected");
                _connectedEvent.Set();
            };
            _ircClient.Registered += (s,e) => {
                _logger.LogDebug("Twitch IRC registered");
                _registeredEvent.Set();
            };
            _ircClient.ErrorMessageReceived += (s,e) => {
                _logger.LogWarning($"ERROR: {e.Message}");
            };
            _ircClient.RawMessageReceived += (s,e) => {
                _logger.LogDebug($"RAW: {e.RawContent}");
            };

            ConnectIrc(cancellationToken);

            // Startup the message posting queue
            while (!cancellationToken.IsCancellationRequested)
            {
                string message = null;
                while (_messageQueue.TryDequeue(out message))
                {
                    try
                    {
                        _logger.LogDebug($"SendPublicChatMessage: {message}");
                        _ircClient.LocalUser.SendMessage($"#{_twitchConfig.ChannelName.ToLower()}", message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SendPublicChatMessage");
                    }
                }

                await Task.Delay(1000);
            }
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping TwitchIrcService");
            _shutdown = true;
            _ircClient?.Disconnect();

            await Task.CompletedTask;
        }

        private void ConnectIrc(CancellationToken cancellationToken)
        {
            if (!_twitchConfig.Enabled)
                return;

            try
            {
                using (_registeredEvent = new ManualResetEventSlim(false))
                {
                    using (_connectedEvent = new ManualResetEventSlim(false))
                    {  
                        _ircClient.Connect(_twitchConfig.IrcServer, _twitchConfig.IrcPort, false,
                            new IrcUserRegistrationInfo() {
                                Password = _twitchOAuthKey,
                                NickName = _twitchConfig.Username,
                                UserName = _twitchConfig.Username,
                            });

                        // Wait for the connection
                        if (!_connectedEvent.Wait(WAIT_TIMEOUT, cancellationToken))
                        {
                            _logger.LogError("Twitch IRC connection timeout");
                            _twitchConfig.Enabled = false;
                        }
                    }

                    // Wait for the client registration
                    if (!_registeredEvent.Wait(WAIT_TIMEOUT, cancellationToken))
                    {
                        _logger.LogError("Twitch IRC registration timeout");
                        _twitchConfig.Enabled = false;
                    }

                    // Finally, join the channel
                    _ircClient.Channels.Join($"#{_twitchConfig.ChannelName.ToLower()}");
                    _logger.LogDebug($"Joining #{_twitchConfig.ChannelName.ToLower()}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TwitchIrcService");
            }
        }

        public void SendPublicChatMessage(string message)
        {
            if (!_twitchConfig.Enabled)
                return;

            _messageQueue.Enqueue(message);
        }
    }
}