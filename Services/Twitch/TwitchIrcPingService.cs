using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource.Services.Twitch
{

    public class TwitchIrcPingService : BackgroundService
    {
        // 5 minute wait in between pings
        private const int SLEEP_INTERVAL = 300000;
        private ILogger<TwitchIrcPingService> Logger {get;}
        private TwitchIrcService IrcService {get;}

        public TwitchIrcPingService(ILogger<TwitchIrcPingService> logger, TwitchIrcService ircService)
        {
            Logger = logger;
            IrcService = ircService;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("TwitchIrcPingService stopped");
            await Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("TwitchIrcPingService started");

            while (!cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("PING irc.twitch.tv");
                IrcService.SendIrcMessage("PING irc.twitch.tv");
                
                await Task.Delay(SLEEP_INTERVAL, cancellationToken);
            }

            await Task.CompletedTask;
        }
    }
}
