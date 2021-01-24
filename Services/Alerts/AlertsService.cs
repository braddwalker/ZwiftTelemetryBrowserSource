using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZwiftTelemetryBrowserSource.Services.Alerts
{
    public interface IAlertsNotificationService
    {
        Task SendNotificationAsync(string notification);
    }

    public class AlertsService 
    {
        private IDictionary<string, IAlertsNotificationService> _alerts;

        public AlertsService()
        {
            _alerts = new Dictionary<string, IAlertsNotificationService>();
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
}