using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource.Services
{
    public abstract class BaseZwiftService : BackgroundService
    {
        private readonly ILogger<BaseZwiftService> _logger;

        public BaseZwiftService(ILogger<BaseZwiftService> logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
        }

        public async override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {this.GetType().Name}");
            await base.StartAsync(cancellationToken);
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stopping {this.GetType().Name}");
            await base.StopAsync(cancellationToken);
        }
    }
}