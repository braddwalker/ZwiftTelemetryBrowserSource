using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ZwiftTelemetryBrowserSource
{
    public class HeartRateRequest {
        private readonly RequestDelegate _next;

        public HeartRateRequest(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ZwiftTelemetry zwiftTelemetry)
        {
            await HttpResponseWritingExtensions.WriteAsync(context.Response, $"<html><head><meta http-equiv=\"refresh\" content=\"5\"></head><body style=\"margin:0;\">PlayerId: {zwiftTelemetry.PlayerState.Id}\n<br/>HR: {zwiftTelemetry.PlayerState.Heartrate}\n<br/>Power: {zwiftTelemetry.PlayerState.Power}\n<br/>Distance: {zwiftTelemetry.PlayerState.Distance}</body></html>");
        }
    }
}