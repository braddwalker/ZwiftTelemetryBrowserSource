using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal class TelemetryNotificationsService : NotificationsServiceBase, ITelemetryNotificationsService
    {
        #region Constructor
        public TelemetryNotificationsService(ITelemetryNotificationsSSEService notificationsServerSentEventsService)
            : base(notificationsServerSentEventsService)
        { }
        #endregion

        #region Methods
        public Task SendNotificationAsync(string notification)
        {
            return SendSseEventAsync(notification);
        }
        #endregion
    }
}