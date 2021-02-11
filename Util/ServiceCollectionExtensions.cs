using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ZwiftTelemetryBrowserSource.Util
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// This extension method simplifies the process of registering an <c>IHostedService</c> type
        /// that also needs to be registered as a singleton for injection into other services. 
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        public static IServiceCollection AddSingletonHostedService<T>(this IServiceCollection services) where T : class, IHostedService
        {
            return (services
                .AddSingleton<T>()
                .AddHostedService<T>(provider => provider.GetService<T>()));
        }
    }
}
