using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal class RideOnNotificationService : NotificationsServiceBase, IRideOnNotificationService
    {
        #region Constructor
        public RideOnNotificationService(IRideOnNotificationsSSEService notificationsServerSentEventsService)
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