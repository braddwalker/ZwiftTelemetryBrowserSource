using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZwiftTelemetryBrowserSource.Services.Notifications;
using ZwiftTelemetryBrowserSource.Models;
using Newtonsoft.Json;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class TelemetryService : BaseZwiftService
    {
        private readonly ILogger<TelemetryService> _logger;
        private readonly ZwiftMonitorService _zwiftService;
        private readonly ITelemetryNotificationsService _telemetryNotificationsService;
        private readonly AverageTelemetryService _averageTelemetryService;

        public TelemetryService(ILogger<TelemetryService> logger, ZwiftMonitorService zwiftService, 
            ITelemetryNotificationsService telemetryNotificationService,
            AverageTelemetryService averageTelemetryService) : base(logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _zwiftService = zwiftService ?? throw new ArgumentException(nameof(zwiftService));
            _telemetryNotificationsService = telemetryNotificationService ?? throw new ArgumentException(nameof(telemetryNotificationService));
            _averageTelemetryService = averageTelemetryService ?? throw new ArgumentException(nameof(averageTelemetryService));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _zwiftService.OutgoingPlayerEvent += async (s, e) =>
            {
                var summary = _averageTelemetryService.LogTelemetry(e.PlayerState);

                var telemetry = JsonConvert.SerializeObject(new TelemetryModel()
                {
                    PlayerId = e.PlayerState.Id,
                    Power = e.PlayerState.Power,
                    HeartRate = e.PlayerState.Heartrate,
                    AvgPower = summary.Power,
                    AvgHeartRate = summary.Heartrate,
                    AvgSpeed = summary.Speed,
                    AvgCadence = summary.Cadence
                });

                await _telemetryNotificationsService.SendNotificationAsync(telemetry);
            };

            await Task.CompletedTask;
        }
    }
}