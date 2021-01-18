using Lib.AspNetCore.ServerSentEvents;
using Microsoft.Extensions.Options;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal class TelemetryNotificationsSSEService : ServerSentEventsService, ITelemetryNotificationsSSEService
    {
        public TelemetryNotificationsSSEService(IOptions<ServerSentEventsServiceOptions<TelemetryNotificationsSSEService>> options)
            : base(options.ToBaseServerSentEventsServiceOptions())
        { }
    }
}