using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services
{
    public interface INotificationsService
    {
        Task SendNotificationAsync(string notification, bool alert);
    }
}