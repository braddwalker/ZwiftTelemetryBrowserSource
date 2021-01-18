using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    public interface IChatNotificationsService
    {
        Task SendNotificationAsync(string notification);
    }
}