using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    public interface IRideOnNotificationService
    {
        Task SendNotificationAsync(string notification, bool alert);
    }
}