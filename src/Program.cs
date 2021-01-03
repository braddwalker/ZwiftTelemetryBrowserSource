using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddCommandLine(args).Build();
            var builder = new WebHostBuilder()
                .ConfigureLogging(logging => {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .UseConfiguration(config)
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://*:89");

                var host = builder.Build();
                host.Run();
        }
    }
}
