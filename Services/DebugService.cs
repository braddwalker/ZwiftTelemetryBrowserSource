using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class DebugService : BaseZwiftService
    {
        private readonly ILogger<DebugService> _logger;
        private readonly ZwiftMonitorService _zwiftService;

        public DebugService(ILogger<DebugService> logger, ZwiftMonitorService zwiftService) : base(logger)
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

            _zwiftService.IncomingPlayerTimeSyncEvent +=  (s, e) =>
            {
                _logger.LogDebug($"IncomingPlayerTimeSyncEvent: {e.TimeSync}");
            };

            _zwiftService.IncomingRideOnGivenEvent += (s, e) =>
            {
                _logger.LogDebug($"IncomingRideOnGivenEvent: {e.RideOn}");
            };

            _zwiftService.OutgoingPlayerEvent += (s, e) =>
            {
                _logger.LogDebug($"OutgoingPlayerEvent: {e.PlayerState}");
            };

            _zwiftService.IncomingMeetupEvent += (s, e) =>
            {
                _logger.LogDebug($"IncomingMeetupEvent: {e.Meetup}");
            };

            _zwiftService.IncomingEventPositionsEvent += (s, e) =>
            {
                _logger.LogDebug($"IncomingEventPositionsEvent: {e.EventPositions}");          
            };

            await Task.CompletedTask;
        }
    }
}