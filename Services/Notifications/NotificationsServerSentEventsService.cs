using Lib.AspNetCore.ServerSentEvents;
using Microsoft.Extensions.Options;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal class NotificationsServerSentEventsService : ServerSentEventsService, INotificationsServerSentEventsService
    {
        public NotificationsServerSentEventsService(IOptions<ServerSentEventsServiceOptions<NotificationsServerSentEventsService>> options)
            : base(options.ToBaseServerSentEventsServiceOptions())
        { }
    }
}