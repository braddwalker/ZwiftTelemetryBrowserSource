using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ZwiftTelemetryBrowserSource.Services
{
    internal static class ServiceCollectionExtensions
    {
        #region Fields
        private const string NOTIFICATIONS_SERVICE_TYPE_CONFIGURATION_KEY = "NotificationsService";
        private const string NOTIFICATIONS_SERVICE_TYPE_LOCAL = "Local";
        #endregion

        #region Methods
        public static IServiceCollection AddNotificationsService(this IServiceCollection services, IConfiguration configuration)
        {
            string notificationsServiceType = configuration.GetValue(NOTIFICATIONS_SERVICE_TYPE_CONFIGURATION_KEY, NOTIFICATIONS_SERVICE_TYPE_LOCAL);

            if (notificationsServiceType.Equals(NOTIFICATIONS_SERVICE_TYPE_LOCAL, StringComparison.InvariantCultureIgnoreCase))
            {
                services.AddTransient<INotificationsService, LocalNotificationsService>();
            }
            else
            {
                throw new NotSupportedException($"Not supported {nameof(INotificationsService)} type.");
            }

            return services;
        }
        #endregion
    }
}