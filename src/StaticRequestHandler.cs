using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource
{
    public class StaticRequestHandler {
        private readonly RequestDelegate _next;
        private readonly ILogger<StaticRequestHandler> _logger;
        public StaticRequestHandler(ILogger<StaticRequestHandler> logger, RequestDelegate next)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ZwiftTelemetry zwiftTelemetry)
        {
            var html = File.ReadAllText($"./src/static{context.Request.Path}");
            await HttpResponseWritingExtensions.WriteAsync(context.Response, html);
        }
    }
}