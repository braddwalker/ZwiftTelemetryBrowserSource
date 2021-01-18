using Lib.AspNetCore.ServerSentEvents;
using Microsoft.Extensions.Options;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal class ChatNotificationsSSEService : ServerSentEventsService, IChatNotificationsSSEService
    {
        public ChatNotificationsSSEService(IOptions<ServerSentEventsServiceOptions<ChatNotificationsSSEService>> options)
            : base(options.ToBaseServerSentEventsServiceOptions())
        { }
    }
}