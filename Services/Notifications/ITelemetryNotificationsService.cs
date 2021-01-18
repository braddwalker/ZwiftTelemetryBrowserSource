using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    public interface ITelemetryNotificationsService
    {
        Task SendNotificationAsync(string notification);
    }
}