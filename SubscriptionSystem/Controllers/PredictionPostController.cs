using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SubscriptionSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication
    public class PredictionPostController : ControllerBase
    {
        private readonly IPredictionPostService _predictionPostService;
        private readonly ILogger<PredictionPostController> _logger;

        public PredictionPostController(
            IPredictionPostService predictionPostService,
            ILogger<PredictionPostController> logger)
        {
            _predictionPostService = predictionPostService ?? throw new ArgumentNullException(nameof(predictionPostService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Post a daily prediction tip (max 500 characters total).
        /// Only today's predictions can be posted. System will send to 100 naira daily subscribers via MO.
        /// </summary>
        /// <param name="request">Team1, Team2, and PredictionOutcome (alphanumeric, max 500 chars combined)</param>
        [HttpPost("daily")]
        public async Task<IActionResult> PostDailyPrediction([FromBody] DailyPredictionPostDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body is required." });

                if (string.IsNullOrWhiteSpace(request.Team1) || string.IsNullOrWhiteSpace(request.Team2) || string.IsNullOrWhiteSpace(request.PredictionOutcome))
                    return BadRequest(new { message = "Team1, Team2, and PredictionOutcome are required fields." });

                // Calculate total character count
                var totalChars = (request.Team1?.Length ?? 0) + (request.Team2?.Length ?? 0) + (request.PredictionOutcome?.Length ?? 0);
                if (totalChars > 500)
                    return BadRequest(new { message = $"Total character count ({totalChars}) exceeds 500 character limit." });

                // Only allow posting for today
                var today = DateTime.UtcNow.Date;
                var result = await _predictionPostService.CreateDailyPredictionAsync(request.Team1, request.Team2, request.PredictionOutcome, today);

                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                return Ok(new 
                { 
                    message = "Daily prediction posted successfully.",
                    predictionId = result.PredictionId,
                    postedDate = today.ToString("yyyy-MM-dd"),
                    team1 = request.Team1,
                    team2 = request.Team2,
                    predictionOutcome = request.PredictionOutcome
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting daily prediction");
                return StatusCode(500, new { message = "An error occurred while posting prediction.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get today's prediction (for testing/verification)
        /// </summary>
        [HttpGet("today")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTodaysPrediction()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var prediction = await _predictionPostService.GetPredictionForDateAsync(today);

                if (prediction == null)
                    return NotFound(new { message = "No prediction available for today." });

                return Ok(prediction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's prediction");
                return StatusCode(500, new { message = "An error occurred.", detail = ex.Message });
            }
        }
    }
}
