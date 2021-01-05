using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json").Build();
            var builder = new WebHostBuilder()
                .ConfigureLogging(logging => {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .UseConfiguration(config)
                .UseKestrel()
                .UseStartup<Startup>();
                
                var host = builder.Build();
                host.Run();
        }
    }
}
