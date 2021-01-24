using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource.Controllers
{
    public class RideOnController : Controller
    {
        private readonly ILogger<RideOnController> _logger;

        public RideOnController(ILogger<RideOnController> logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
