using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lib.AspNetCore.ServerSentEvents;

namespace ZwiftTelemetryBrowserSource.Services.Alerts
{
    public interface IAlertsNotificationService
    {
        Task SendNotificationAsync(string notification);
    }

    public class AlertsService
    {
        private IDictionary<string, IAlertsNotificationService> _alerts;
        private ILogger<AlertsService> _logger;
        private AlertsConfig _config;

        public AlertsService(ILogger<AlertsService> logger, IOptions<AlertsConfig> config)
        {
            _config = config?.Value ?? throw new ArgumentException(nameof(config));
            _alerts = new Dictionary<string, IAlertsNotificationService>();
            _logger = logger;
        }

        public void RegisterAlert(string alertName, IAlertsNotificationService service) 
        {
            if (_alerts.ContainsKey(alertName)) 
            {
                throw new ArgumentException($"{alertName} already registered");
            }
            else
            {
                _alerts.Add(alertName, service);
            }
        }

        public async Task SendNotificationAsync(string alertName, string notification)
        {
            if (_alerts.ContainsKey(alertName))
            {
                await _alerts[alertName].SendNotificationAsync(notification);
            }
            else
            {
                throw new ArgumentException($"{alertName} not registered");
            }
        }
    }

    internal class AlertsServiceSSE : ServerSentEventsService
    {
        public AlertsServiceSSE(IOptions<ServerSentEventsServiceOptions<AlertsServiceSSE>> options)
            : base(options.ToBaseServerSentEventsServiceOptions())
        { 
        }
    }
}