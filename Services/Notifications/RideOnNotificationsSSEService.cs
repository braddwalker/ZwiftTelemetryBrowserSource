using Lib.AspNetCore.ServerSentEvents;
using Microsoft.Extensions.Options;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal class RideOnNotificationsSSEService : ServerSentEventsService, IRideOnNotificationsSSEService
    {
        public RideOnNotificationsSSEService(IOptions<ServerSentEventsServiceOptions<RideOnNotificationsSSEService>> options)
            : base(options.ToBaseServerSentEventsServiceOptions())
        { }
    }
}