using System;
using ZwiftPacketMonitor;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class EventChangedArgs : EventArgs {
        public int OldEventId {get; set;}
        public int NewEventId {get; set;}
    }

    /// <summary>
    /// This service is responsible for detecting event changes and dispatching them to any
    /// consumers who listen for that event.
    /// </summary>
    public class EventService : BaseZwiftService
    {
        private int _currentEventId;
        private readonly ILogger<EventService> _logger;
        private readonly ZwiftMonitorService _zwiftService;

        /// <summary>
        /// Gets fired when the rider's current event changes
        /// </summary>
        public event EventHandler<EventChangedArgs> EventChanged;

        public EventService(ILogger<EventService> logger, ZwiftMonitorService zwiftService) : base(logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _zwiftService = zwiftService ?? throw new ArgumentException(nameof(zwiftService));
            _currentEventId = 0;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _zwiftService.OutgoingPlayerEvent += (s, e) =>
            {
                HandlePlayerEvent(e.PlayerState);
            };

            await Task.CompletedTask;
        }

        /// <summary>
        /// Processes all outgoing player state events, and looks for any changes
        /// in the current GroupId (in-game event). Will dispatch a notification 
        /// to all listeners of <c>EventChanged</c>.
        /// </summary>
        /// <param name="state">Current player state</param>
        private void HandlePlayerEvent(PlayerState state)
        {
            if (_currentEventId != state.GroupId)
            {
                _logger.LogInformation($"Event change detected from {_currentEventId} to {state.GroupId}");

                var e = new EventChangedArgs() 
                {
                    OldEventId = _currentEventId,
                    NewEventId = state.GroupId
                };

                _currentEventId = state.GroupId;

                // Dispatch this update to downstream listeners
                EventHandler<EventChangedArgs> handler = EventChanged;
                if (handler != null)
                {
                    try {
                        handler(this, e);
                    }
                    catch {
                        // Don't let downstream exceptions bubble up
                    }
                }
            }
        }
    }
}