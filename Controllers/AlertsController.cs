using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource.Controllers
{
    public class AlertsController : Controller
    {
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(ILogger<AlertsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Chat()
        {
            return View();
        }

        public IActionResult RideOn()
        {
            return View();
        }
    }
}
