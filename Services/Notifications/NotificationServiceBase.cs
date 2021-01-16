using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lib.AspNetCore.ServerSentEvents;

namespace ZwiftTelemetryBrowserSource.Services.Notifications
{
    internal abstract class NotificationsServiceBase
    {
        #region Fields
        private IServerSentEventsService _notificationsServerSentEventsService;
        #endregion

        #region Constructor
        protected NotificationsServiceBase(IServerSentEventsService notificationsServerSentEventsService)
        {
            _notificationsServerSentEventsService = notificationsServerSentEventsService;
        }
        #endregion

        #region Methods
        protected Task SendSseEventAsync(string notification, bool alert)
        {
            return _notificationsServerSentEventsService.SendEventAsync(new ServerSentEvent
            {
                Type = alert ? "alert" : null,
                Data = new List<string>(notification.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            });
        }
        #endregion
    }
}