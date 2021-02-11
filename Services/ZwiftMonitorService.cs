using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System;
using ZwiftPacketMonitor;

namespace ZwiftTelemetryBrowserSource.Services
{
    /// <summary>
    /// This service is responsible for listening for Zwift game events, dispatched by the ZwiftPacketMonitor
    /// library. When game events are received, they then get dispatched to various listeners that handle 
    /// more specific functionality.
    /// </summary>
    public class ZwiftMonitorService : BaseZwiftService
    {
        public ZwiftMonitorService(ILogger<ZwiftMonitorService> logger, 
            ZwiftPacketMonitor.Monitor zwiftPacketMonitor,
            IConfiguration config) : base(logger)
        {
            _config = config ?? throw new ArgumentException(nameof(config));
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _zwiftPacketMonitor = zwiftPacketMonitor ?? throw new ArgumentException(nameof(zwiftPacketMonitor));
        }

        public event EventHandler<PlayerStateEventArgs> IncomingPlayerEvent;
        public event EventHandler<PlayerStateEventArgs> OutgoingPlayerEvent;
        public event EventHandler<PlayerEnteredWorldEventArgs> IncomingPlayerEnteredWorldEvent;
        public event EventHandler<RideOnGivenEventArgs> IncomingRideOnGivenEvent;
        public event EventHandler<ChatMessageEventArgs> IncomingChatMessageEvent;

        public event EventHandler<PlayerWorldTimeEventArgs> IncomingPlayerWorldTimeUpdateEvent;

        private IConfiguration _config;
        private ILogger<ZwiftMonitorService> _logger;
        private ZwiftPacketMonitor.Monitor _zwiftPacketMonitor;
        
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _zwiftPacketMonitor.StopCaptureAsync();
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {            
            _zwiftPacketMonitor.OutgoingPlayerEvent += (s, e) => {
                EventHandler<PlayerStateEventArgs> handler = OutgoingPlayerEvent;
                if (handler != null)
                {
                    try {
                        handler(this, e);
                    }
                    catch {
                        // Don't let downstream exceptions bubble up
                    }
                }
            };

            _zwiftPacketMonitor.IncomingPlayerEvent += (s, e) => {
                EventHandler<PlayerStateEventArgs> handler = IncomingPlayerEvent;
                if (handler != null)
                {
                    try {
                        handler(this, e);
                    }
                    catch {
                        // Don't let downstream exceptions bubble up
                    }
                }                
            };

            _zwiftPacketMonitor.IncomingPlayerWorldTimeUpdateEvent += (s, e) => {
                EventHandler<PlayerWorldTimeEventArgs> handler = IncomingPlayerWorldTimeUpdateEvent;
                if (handler != null)
                {
                    try {
                        handler(this, e);
                    }
                    catch {
                        // Don't let downstream exceptions bubble up
                    }
                }
            };
            
            _zwiftPacketMonitor.IncomingChatMessageEvent += (s, e) => {
                EventHandler<ChatMessageEventArgs> handler = IncomingChatMessageEvent;
                if (handler != null)
                {
                    try {
                        handler(this, e);
                    }
                    catch {
                        // Don't let downstream exceptions bubble up
                    }
                }
            };

            _zwiftPacketMonitor.IncomingPlayerEnteredWorldEvent += (s, e) => {
                EventHandler<PlayerEnteredWorldEventArgs> handler = IncomingPlayerEnteredWorldEvent;
                if (handler != null)
                {
                    try {
                        handler(this, e);
                    }
                    catch {
                        // Don't let downstream exceptions bubble up
                    }
                }
            };

            _zwiftPacketMonitor.IncomingRideOnGivenEvent += (s, e) => {
                EventHandler<RideOnGivenEventArgs> handler = IncomingRideOnGivenEvent;
                if (handler != null)
                {
                    try {
                        handler(this, e);
                    }
                    catch {
                        // Don't let downstream exceptions bubble up
                    }
                }
            };

            await _zwiftPacketMonitor.StartCaptureAsync(_config.GetValue<string>("NetworkInterface"), cancellationToken);
        }
    }
}