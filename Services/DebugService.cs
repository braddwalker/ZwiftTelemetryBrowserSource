using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class DebugService : BackgroundService
    {
        private readonly ILogger<DebugService> _logger;
        private readonly ZwiftMonitorService _zwiftService;

        public DebugService(ILogger<DebugService> logger, ZwiftMonitorService zwiftService)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _zwiftService = zwiftService ?? throw new ArgumentException(nameof(zwiftService));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _zwiftService.IncomingChatMessageEvent += (s, e) =>
            {
                _logger.LogDebug($"IncomingChatMessage: {e.Message}");
            };

            _zwiftService.IncomingPlayerEnteredWorldEvent += (s, e) =>
            {
                _logger.LogDebug($"IncomingPlayerEnteredWorldEvent: {e.PlayerUpdate}");
            };

            _zwiftService.IncomingPlayerEvent += (s, e) =>
            {
                _logger.LogDebug($"IncomingPlayerEvent: {e.PlayerState}");
            };

            _zwiftService.IncomingPlayerWorldTimeUpdateEvent +=  (s, e) =>
            {
                _logger.LogDebug($"IncomingPlayerWorldTimeUpdateEvent: {e.PlayerUpdate}");
            };

            _zwiftService.IncomingRideOnGivenEvent += (s, e) =>
            {
                _logger.LogDebug($"IncomingRideOnGivenEvent: {e.RideOn}");
            };

            _zwiftService.OutgoingPlayerEvent += (s, e) =>
            {
                _logger.LogDebug($"OutgoingPlayerEvent: {e.PlayerState}");
            };

            await Task.CompletedTask;
        }
    }
}