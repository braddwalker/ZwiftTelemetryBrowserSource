using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZwiftTelemetryBrowserSource
{
    public class OverviewRequestHandler {
        private readonly RequestDelegate _next;
        private readonly ILogger<PowerRequestHandler> _logger;
        private readonly Zones _zones;
        public OverviewRequestHandler(ILogger<PowerRequestHandler> logger, 
            IOptions<Zones> settings,
            RequestDelegate next)
        {
            _next = next;
            _logger = logger;
            _zones = settings.Value;
        }

        public async Task InvokeAsync(HttpContext context, ZwiftTelemetry zwiftTelemetry)
        {
            var html = File.ReadAllText("./src/html/overview-gauge.html")
                .Replace("$Power.Z1.Min", "0")
                .Replace("$Power.Z1.Max", _zones.Power.Z1.ToString())
                .Replace("$Power.Z2.Min", (_zones.Power.Z1 + 1).ToString())
                .Replace("$Power.Z2.Max", _zones.Power.Z2.ToString())
                .Replace("$Power.Z3.Min", (_zones.Power.Z2 + 1).ToString())
                .Replace("$Power.Z3.Max", _zones.Power.Z3.ToString())
                .Replace("$Power.Z4.Min", (_zones.Power.Z3 + 1).ToString())
                .Replace("$Power.Z4.Max", _zones.Power.Z4.ToString())
                .Replace("$Power.Z5.Min", (_zones.Power.Z4 + 1).ToString())
                .Replace("$Power.Z5.Max", _zones.Power.Z5.ToString())
                .Replace("$Power.Z6.Min", (_zones.Power.Z5 + 1).ToString())
                .Replace("$Power.Z6.Max", _zones.Power.Z6.ToString())
                .Replace("$Power.Z7.Min", (_zones.Power.Z6 + 1).ToString())
                .Replace("$Power.Z7.Max", _zones.Power.Z7.ToString())

                .Replace("$HR.Z1.Min", "0")
                .Replace("$HR.Z1.Max", _zones.HR.Z1.ToString())
                .Replace("$HR.Z2.Min", (_zones.HR.Z1 + 1).ToString())
                .Replace("$HR.Z2.Max", _zones.HR.Z2.ToString())
                .Replace("$HR.Z3.Min", (_zones.HR.Z2 + 1).ToString())
                .Replace("$HR.Z3.Max", _zones.HR.Z3.ToString())
                .Replace("$HR.Z4.Min", (_zones.HR.Z3 + 1).ToString())
                .Replace("$HR.Z4.Max", _zones.HR.Z4.ToString())
                .Replace("$HR.Z5.Min", (_zones.HR.Z4 + 1).ToString())
                .Replace("$HR.Z5.Max", _zones.HR.Z5.ToString())
                .Replace("$HR.Z6.Min", (_zones.HR.Z5 + 1).ToString())
                .Replace("$HR.Z6.Max", _zones.HR.Z6.ToString());

            await HttpResponseWritingExtensions.WriteAsync(context.Response, html);
        }
    }
}