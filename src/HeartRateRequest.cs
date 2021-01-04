using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource
{
    public class HeartRateRequest {
        private readonly RequestDelegate _next;
        private readonly ILogger<PowerRequest> _logger;
        public HeartRateRequest(ILogger<PowerRequest> logger, RequestDelegate next)
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
                    html = File.ReadAllText("./src/hr-gauge.html");
                }
            }

            await HttpResponseWritingExtensions.WriteAsync(context.Response, html);
        }
    }
}