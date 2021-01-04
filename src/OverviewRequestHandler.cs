using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource
{
    public class OverviewRequestHandler {
        private readonly RequestDelegate _next;
        private readonly ILogger<PowerRequestHandler> _logger;
        public OverviewRequestHandler(ILogger<PowerRequestHandler> logger, RequestDelegate next)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ZwiftTelemetry zwiftTelemetry)
        {
            var html = File.ReadAllText("./src/html/overview-gauge.html");
            await HttpResponseWritingExtensions.WriteAsync(context.Response, html);
        }
    }
}