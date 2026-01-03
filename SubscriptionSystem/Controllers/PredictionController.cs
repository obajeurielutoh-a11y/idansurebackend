using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PredictionController : ControllerBase
    {
        private readonly IPredictionService _predictionService;
        private readonly IAsedeyhotPredictionService _asedeyhotPredictionService;
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(
            IPredictionService predictionService,
            IAsedeyhotPredictionService asedeyhotPredictionService,
            IUserManagementService userManagementService,
            ILogger<PredictionController> logger)
        {
            _predictionService = predictionService;
            _asedeyhotPredictionService = asedeyhotPredictionService;
            _userManagementService = userManagementService;
            _logger = logger;
        }

        [HttpDelete("Asedeyhot/{id}")]
        [Authorize(AuthenticationSchemes = "Basic")]

        public async Task<IActionResult> DeleteAsedeyhotPrediction(Guid id)
        {
            var result = await _asedeyhotPredictionService.DeletePredictionAsync(id);
            if (result.IsSuccess)
                return Ok(new { message = "Asedeyhot prediction deleted successfully" });
            return BadRequest(new { message = result.Message });
        }
        private async Task<bool> HasActiveSubscriptionAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("User email is missing");
                return false;
            }

            var user = await _userManagementService.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"User with email {email} not found");
                return false;
            }

            // Check if the user is active
            if (!user.IsActive)
            {
                _logger.LogWarning($"User with email {email} is not active");
                return false;
            }

            // Prefer explicit subscription flags/expiry over authentication tokens for anonymous checks
            if (user.HasActiveSubscription)
            {
                return true;
            }

            if (user.SubscriptionExpiry.HasValue && user.SubscriptionExpiry.Value > DateTime.UtcNow)
            {
                return true;
            }

            _logger.LogWarning($"User with email {email} does not have an active subscription");
            return false;
        }
        [HttpPost("{id}/outcome")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> UpdatePredictionOutcome(string id, [FromBody] UpdateOutcomeDto updateOutcomeDto)
        {
            var result = await _predictionService.UpdatePredictionOutcomeAsync(id, updateOutcomeDto.Outcome);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok();
        }


        [HttpGet]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetPredictions([FromQuery] string email, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Attempting to fetch predictions. Email: {email}, Page: {page}, PageSize: {pageSize}");

                if (!await HasActiveSubscriptionAsync(email))
                {
                    return Unauthorized("Active subscription required to view predictions.");
                }

                var result = await _predictionService.GetPredictionsAsync(page, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching predictions");

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpGet("Admin")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> GetAdminPredictions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Attempting to fetch admin predictions. Page: {page}, PageSize: {pageSize}");

                var result = await _predictionService.GetPredictionsAsync(page, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching admin predictions");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("Past")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetPastPredictions([FromQuery] string email, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!await HasActiveSubscriptionAsync(email))
            {
                return Unauthorized("Active subscription required to view predictions.");
            }

            var result = await _predictionService.GetPastPredictionsAsync(page, pageSize);
            return Ok(result);
        }


        [HttpGet("Statistics")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetPredictionStatistics([FromQuery] string email)
        {
            if (!await HasActiveSubscriptionAsync(email))
            {
                return Unauthorized("Active subscription required to view prediction statistics.");
            }

            var result = await _predictionService.GetPredictionStatisticsAsync();
            return Ok(result);
        }

        [HttpPost("DetailedPrediction")]
        // [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> PostDetailedPrediction([FromBody] DetailedPredictionDto predictionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _predictionService.CreateDetailedPredictionAsync(predictionDto);
            if (result.IsSuccess)
                return Ok(new { message = "Detailed prediction created successfully", id = result.Data });
            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("SimplePrediction")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> PostSimplePrediction([FromBody] SimplePredictionDto predictionDto)
        {
            var result = await _predictionService.CreateSimplePredictionAsync(predictionDto);
            if (result.IsSuccess)
                return Ok(new { message = "Simple prediction created successfully", id = result.Data });
            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Basic")]

        public async Task<IActionResult> UpdatePrediction(string id, [FromBody] DetailedPredictionDto predictionDto)
        {
            var result = await _predictionService.UpdatePredictionAsync(id, predictionDto);
            if (result.IsSuccess)
                return Ok(new { message = "Prediction updated successfully" });
            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> DeletePrediction(string id)
        {
            var result = await _predictionService.DeletePredictionAsync(id);
            if (result.IsSuccess)
                return Ok(new { message = "Prediction deleted successfully" });
            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("Asedeyhot")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> PostAsedeyhotPrediction([FromBody] AsedeyhotPredictionDto AsedeyhotPredictionDto)
        {
            var result = await _asedeyhotPredictionService.CreatePredictionAsync(AsedeyhotPredictionDto);
            if (result.IsSuccess)
                return Ok(new { message = "Asedeyhot prediction created successfully", data = result.Data });
            return BadRequest(new { message = result.Message });
        }

        [HttpPut("Asedeyhot/{id}/Result")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> UpdateAsedeyhotPredictionResult(Guid id, [FromBody] AsedeyhotPredictionResultUpdateDto updateDto)
        {
            var result = await _asedeyhotPredictionService.UpdatePredictionResultAsync(id, updateDto.IsWin, updateDto.ResultDetails);
            if (result.IsSuccess)
                return Ok(new { message = "Asedeyhot prediction result updated successfully", data = result.Data });
            return BadRequest(new { message = result.Message });
        }

        [HttpGet("Asedeyhot")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAsedeyhotPredictions([FromQuery] string email, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!await HasActiveSubscriptionAsync(email))
            {
                return Unauthorized("Active subscription required to view predictions.");
            }
            var result = await _asedeyhotPredictionService.GetPredictionsAsync(page, pageSize);
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(new { message = result.Message });
        }

        [HttpGet("Asedeyhot/result/table")]
        //[Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> GetAsedeyhotPrediction([FromQuery] string email, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Fetching Asedeyhot predictions. Email: {email}, Page: {page}, PageSize: {pageSize}");

                var result = await _asedeyhotPredictionService.GetPredictionsAsync(page, pageSize);

                if (result.IsSuccess)
                    return Ok(result.Data);

                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching Asedeyhot predictions");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("Asedeyhot/Statistics")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAsedeyhotPredictionStatistics([FromQuery] string email)
        {
            if (!await HasActiveSubscriptionAsync(email))
            {
                return Unauthorized("Active subscription required to view prediction statistics.");
            }
            var result = await _asedeyhotPredictionService.GetPredictionStatisticsAsync();
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(new { message = result.Message });
        }

        [HttpGet("Asedeyhot/{id}")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetAsedeyhotPredictionById(Guid id, [FromQuery] string email)
        {
            if (!await HasActiveSubscriptionAsync(email))
            {
                return Unauthorized("Active subscription required to view predictions.");
            }
            var result = await _asedeyhotPredictionService.GetPredictionByIdAsync(id);
            if (result.IsSuccess)
                return Ok(result.Data);
            return NotFound(new { message = result.Message });
        }




        [HttpGet("Asedeyhot/PastAndExpired")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPastAndExpiredAsedeyhotPredictions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _asedeyhotPredictionService.GetPastAndExpiredPredictionsAsync(page, pageSize);
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(new { message = result.Message });
        }

        [HttpGet("preferences")]
        public async Task<IActionResult> GetUserPreferences([FromQuery] string email)
        {
            if (!await HasActiveSubscriptionAsync(email))
            {
                return Unauthorized("Active subscription required to view user preferences.");
            }

            var result = await _userManagementService.GetUserPreferencesAsync(email);
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(new { message = result.ErrorMessage });
        }
    }

    public class AsedeyhotPredictionResultUpdateDto
    {
        public bool IsWin { get; set; }
        public string ResultDetails { get; set; }
    }
    public class UpdateOutcomeDto
    {
        public Domain.Entities.MatchOutcome Outcome { get; set; }
    }
}

