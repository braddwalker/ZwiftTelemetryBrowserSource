using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ZwiftTelemetryBrowserSource
{
    public class PowerRequest {
        private readonly RequestDelegate _next;

        public PowerRequest(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ZwiftTelemetry zwiftTelemetry)
        {
            await HttpResponseWritingExtensions.WriteAsync(context.Response, $"<html><head><meta http-equiv=\"refresh\" content=\"5\"></head><body style=\"margin:0;\">Power: {zwiftTelemetry.PlayerState.Power}</body></html>");
        }
    }
}