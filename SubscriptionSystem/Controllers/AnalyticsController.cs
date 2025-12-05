using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System.Threading.Tasks;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IPredictionAnalyticsService _analytics;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IPredictionAnalyticsService analytics, ILogger<AnalyticsController> logger)
        {
            _analytics = analytics;
            _logger = logger;
        }

        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthly([FromQuery] int year, [FromQuery] int month)
        {
            if (year <= 0 || month < 1 || month > 12)
                return BadRequest(new { message = "Invalid year/month" });

            var result = await _analytics.GetMonthlyAnalyticsAsync(year, month);
            return Ok(result);
        }
    }
}
