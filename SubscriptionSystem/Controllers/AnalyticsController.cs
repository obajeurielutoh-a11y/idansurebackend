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
        public async Task<IActionResult> GetMonthly([FromQuery] int? year, [FromQuery] int? month)
        {
            var now = DateTime.UtcNow;
            var useYear = year ?? now.Year;
            var useMonth = month ?? now.Month;

            if (useYear <= 0 || useMonth < 1 || useMonth > 12)
                return BadRequest(new { message = "Invalid year/month" });

            try
            {
                var result = await _analytics.GetMonthlyAnalyticsAsync(useYear, useMonth);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Analytics GetMonthly failed for {Year}/{Month}", useYear, useMonth);
                return StatusCode(503, new { message = "Analytics temporarily unavailable", details = ex.Message });
            }
        }
    }
}
