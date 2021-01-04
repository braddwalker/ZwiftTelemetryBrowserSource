using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource
{
    public class PowerRequest {
        private readonly RequestDelegate _next;
        private readonly ILogger<PowerRequest> _logger;
        public PowerRequest(ILogger<PowerRequest> logger, RequestDelegate next)
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
                    html = zwiftTelemetry.PlayerState.Power.ToString();
                }
                else
                {
                    html = File.ReadAllText("./src/power-gauge.html");
                }
            }

            await HttpResponseWritingExtensions.WriteAsync(context.Response, html);
        }
    }
}