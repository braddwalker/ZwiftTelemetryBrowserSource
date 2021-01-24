using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZwiftTelemetryBrowserSource.Models;

namespace ZwiftTelemetryBrowserSource.Controllers
{
    public class AveragesController : Controller
    {
        private readonly ILogger<AveragesController> _logger;

        public AveragesController(ILogger<AveragesController> logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));

        }

        public IActionResult Power()
        {
            var model = new AveragesModel() {
                DisplayName = "Avg. Power",
                Metric = "AvgPower"
            };

            return View("index", model);
        }

        public IActionResult HR()
        {
            var model = new AveragesModel() {
                DisplayName = "Avg. HR",
                Metric = "AvgHeartRate"
            };

            return View("index", model);
        }

        public IActionResult Cadence()
        {
            var model = new AveragesModel() {
                DisplayName = "Avg. Cadence",
                Metric = "AvgCadence"
            };

            return View("index", model);
        }

        public IActionResult Speed()
        {
            var model = new AveragesModel() {
                DisplayName = "Avg. Speed",
                Metric = "AvgSpeed"
            };

            return View("index", model);
        }

        public IActionResult All()
        {
            return View("all");
        }
    }
}
