using System.Threading.Tasks;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal class ChatNotificationsService : NotificationsServiceBase, IChatNotificationsService
    {
        #region Constructor
        public ChatNotificationsService(IChatNotificationsSSEService notificationsServerSentEventsService)
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