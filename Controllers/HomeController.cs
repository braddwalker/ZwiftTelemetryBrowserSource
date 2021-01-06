using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZwiftTelemetryBrowserSource.Models;

namespace ZwiftTelemetryBrowserSource.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ZonesModel _zones;
        public HomeController(ILogger<HomeController> logger, IOptions<ZonesModel> zones)
        {
            _logger = logger;
            _zones = zones.Value;
        }

        public IActionResult Index()
        {
            return View(_zones);
        }

        public IActionResult API(ZwiftTelemetry zwiftTelemetry)
        {
            return new JsonResult(new TelemetryModel() {
                PlayerId = zwiftTelemetry?.PlayerState?.Id ?? 0,
                Power = zwiftTelemetry?.PlayerState?.Power ?? 0,
                HeartRate = zwiftTelemetry?.PlayerState?.Heartrate ?? 0
            });
        }
    }
}
