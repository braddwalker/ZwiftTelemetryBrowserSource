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

        private IrcDotNet.TwitchIrcClient IrcClient;
        private TwitchConfig TwitchConfig;
        private string TwitchOAuthKey;
        private ILogger<TwitchIrcService> Logger;

        private ManualResetEventSlim connectedEvent;
        private ManualResetEventSlim registeredEvent;
        private bool shutdown;

        private ConcurrentQueue<string> messageQueue;
        

        public TwitchIrcService(ILogger<TwitchIrcService> logger, IOptions<TwitchConfig> twitchConfig)
        {
            TwitchConfig = twitchConfig.Value;
            Logger = logger;
            shutdown = false;
            messageQueue = new ConcurrentQueue<string>();

            if (TwitchConfig.Enabled)
            {
                Logger.LogInformation("TwitchIrcService enabled");

                TwitchOAuthKey = File.ReadAllText(TwitchConfig.AuthTokenFile).Trim();
                Logger.LogInformation($"Twitch OAuth key loaded from {new FileInfo(TwitchConfig.AuthTokenFile).FullName}");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!TwitchConfig.Enabled)
                return;

            Logger.LogInformation("Starting TwitchIrcService");

            IrcClient = new IrcDotNet.TwitchIrcClient();
            IrcClient.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
            IrcClient.Disconnected += (s,e) => {
                Logger.LogDebug("Twitch IRC disconnected");

                // Attempt to reconnect so long as we're not shutting down
                if (!shutdown)
                {
                    Logger.LogDebug("Twitch IRC attempting to reconnect");
                    ConnectIrc(cancellationToken);
                }
            };
            IrcClient.Connected += (s,e) => {
                Logger.LogDebug("Twitch IRC connected");
                connectedEvent.Set();
            };
            IrcClient.Registered += (s,e) => {
                Logger.LogDebug("Twitch IRC registered");
                registeredEvent.Set();
            };
            IrcClient.ErrorMessageReceived += (s,e) => {
                Logger.LogWarning($"ERROR: {e.Message}");
            };
            IrcClient.RawMessageReceived += (s,e) => {
                Logger.LogDebug($"RAW: {e.RawContent}");
            };

            ConnectIrc(cancellationToken);

            // Startup the message posting queue
            while (!cancellationToken.IsCancellationRequested)
            {
                string message = null;
                while (messageQueue.TryDequeue(out message))
                {
                    try
                    {
                        Logger.LogDebug($"SendPublicChatMessage: {message}");
                        IrcClient.LocalUser.SendMessage($"#{TwitchConfig.ChannelName.ToLower()}", message);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "SendPublicChatMessage");
                    }
                }

                await Task.Delay(1000);
            }
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping TwitchIrcService");
            shutdown = true;
            IrcClient.Disconnect();

            await Task.CompletedTask;
        }

        private void ConnectIrc(CancellationToken cancellationToken)
        {
            if (!TwitchConfig.Enabled)
                return;

            try
            {
                using (registeredEvent = new ManualResetEventSlim(false))
                {
                    using (connectedEvent = new ManualResetEventSlim(false))
                    {  
                        IrcClient.Connect(TwitchConfig.IrcServer, TwitchConfig.IrcPort, false,
                            new IrcUserRegistrationInfo() {
                                Password = TwitchOAuthKey,
                                NickName = TwitchConfig.Username,
                                UserName = TwitchConfig.Username,
                            });

                        // Wait for the connection
                        if (!connectedEvent.Wait(WAIT_TIMEOUT, cancellationToken))
                        {
                            Logger.LogError("Twitch IRC connection timeout");
                            TwitchConfig.Enabled = false;
                        }
                    }

                    // Wait for the client registration
                    if (!registeredEvent.Wait(WAIT_TIMEOUT, cancellationToken))
                    {
                        Logger.LogError("Twitch IRC registration timeout");
                        TwitchConfig.Enabled = false;
                    }

                    // Finally, join the channel
                    IrcClient.Channels.Join($"#{TwitchConfig.ChannelName.ToLower()}");
                    Logger.LogDebug($"Joining #{TwitchConfig.ChannelName.ToLower()}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "TwitchIrcService");
            }
        }

        public void SendPublicChatMessage(string message)
        {
            if (!TwitchConfig.Enabled)
                return;

            messageQueue.Enqueue(message);
        }
    }
}