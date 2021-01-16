using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    public interface INotificationsService
    {
        Task SendNotificationAsync(string notification, bool alert);
    }
}