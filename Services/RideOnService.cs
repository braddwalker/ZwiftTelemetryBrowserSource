using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZwiftTelemetryBrowserSource.Services.Notifications;
using ZwiftTelemetryBrowserSource.Models;
using ZwiftTelemetryBrowserSource.Services.Alerts;
using ZwiftTelemetryBrowserSource.Services.Twitch;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class RideOnService : BackgroundService
    {

        private readonly ILogger<RideOnService> _logger;
        private readonly ZwiftMonitorService _zwiftService;
        private readonly IRideOnNotificationService _rideOnNotificationService;
        private readonly AlertsConfig _alertsConfig;
        private readonly TwitchIrcService _twitchIrcService;

        public RideOnService(ILogger<RideOnService> logger, ZwiftMonitorService zwiftService,
            IRideOnNotificationService rideOnNotificationService,
            IOptions<AlertsConfig> alertsConfig,
            TwitchIrcService twitchIrcService)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _zwiftService = zwiftService ?? throw new ArgumentException(nameof(zwiftService));
            _rideOnNotificationService = rideOnNotificationService ?? throw new ArgumentException(nameof(rideOnNotificationService));
            _alertsConfig = alertsConfig?.Value ?? throw new ArgumentException(nameof(alertsConfig));
            _twitchIrcService = twitchIrcService ?? throw new ArgumentException(nameof(twitchIrcService));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _zwiftService.IncomingRideOnGivenEvent += async (s, e) =>
            {
                _logger.LogInformation($"RIDEON: {e.RideOn.ToString()}");
                _twitchIrcService.SendPublicChatMessage($"Thanks for the ride on, {e.RideOn.FirstName} {e.RideOn.LastName}!");

                var message = JsonConvert.SerializeObject(new RideOnNotificationModel()
                {
                    RiderId = e.RideOn.RiderId,
                    FirstName = e.RideOn.FirstName,
                    LastName = e.RideOn.LastName,
                    AudioSource = "/audio/rockon.ogg"
                });

                await _rideOnNotificationService.SendNotificationAsync(message);
            };

            await Task.CompletedTask;
        }
    }
}