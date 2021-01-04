using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource
{
    public class HeartRateRequestHandler {
        private readonly RequestDelegate _next;
        private readonly ILogger<PowerRequestHandler> _logger;
        public HeartRateRequestHandler(ILogger<PowerRequestHandler> logger, RequestDelegate next)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ZwiftTelemetry zwiftTelemetry)
        {
            var html = "<html><body></body></html>";

            if (zwiftTelemetry.PlayerState != null) {
                if (context.Request.QueryString.Value.Contains("dataOnly"))
                {
                    html = zwiftTelemetry.PlayerState.Heartrate.ToString();
                }
                else
                {
                    html = File.ReadAllText("./src/html/hr-gauge.html");
                }
            }

            await HttpResponseWritingExtensions.WriteAsync(context.Response, html);
        }
    }
}