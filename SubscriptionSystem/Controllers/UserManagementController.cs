using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace SubscriptionSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IGroupChatService _groupChatService;
        private readonly IMemoryCache _cache;

        public UserManagementController(IUserManagementService userManagementService, IGroupChatService groupChatService, IMemoryCache cache)
        {
            _userManagementService = userManagementService;
            _groupChatService = groupChatService;
            _cache = cache;
        }       

        [HttpPost("UploadProfilePicture")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _userManagementService.UploadProfilePictureAsync(userId, file);

            if (result.IsSuccess)
                return Ok(new { message = "Profile picture uploaded successfully.", imageUrl = result.Data });
            else
                return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("RequestAccountDeletion/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> RequestAccountDeletion(string userId, [FromBody] AccountDeletionRequestDto request)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { Message = "User ID is required." });
            }

            try
            {
                var message = await _userManagementService.RequestAccountDeletionAsync(userId, request);
                return Ok(new { Message = message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred. Please contact support." });
            }
        }

        [HttpGet("preferences/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetPreferences(string userId)
        {
            var result = await _userManagementService.GetUserPreferencesAsync(userId);

            if (result.IsSuccess)
                return Ok(result.Data);
            else
                return NotFound(new { message = result.ErrorMessage });
        }

        [HttpPost("preferences/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> UpdatePreferences(string userId, [FromBody] UserPreferencesDto preferences)
        {
            var result = await _userManagementService.UpdateUserPreferencesAsync(userId, preferences);

            if (result.IsSuccess)
                return Ok(new { message = "User preferences updated successfully.", data = result.Data });
            else
                return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("ApproveAccountDeletion/{userId}")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> ApproveAccountDeletion(string userId)
        {
            var result = await _userManagementService.ApproveAccountDeletionAsync(userId);

            if (result.IsSuccess)
                return Ok(new { message = "Account deleted successfully." });
            else
                return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("Users")]
        //[Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _userManagementService.GetUsersAsync(page, pageSize);
            return Ok(result);
        }

        [HttpPut("Users/{userId}")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDto request)
        {
            var result = await _userManagementService.UpdateUserAsync(userId, request);
            if (result.IsSuccess)
                return Ok(new { message = "User updated successfully." });
            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("Users/{userId}/ToggleActivation")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> ToggleUserActivation(string userId)
        {
            var result = await _userManagementService.ToggleUserActivationAsync(userId);
            if (result.IsSuccess)
                return Ok(new { message = "User activation status toggled successfully." });
            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("Users/{userId}/PaymentStatus")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> GetUserPaymentStatus(string userId)
        {
            var result = await _userManagementService.GetUserPaymentStatusAsync(userId);
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("Statistics")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics(
    [FromQuery] string timeRange = "30d",
    [FromQuery] string userType = "all")
        {
            try
            {
                var statistics = await _userManagementService.GetUserStatisticsAsync(timeRange, userType);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("Users/{userId}")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await _userManagementService.DeleteUserAsync(userId);
            if (result.IsSuccess)
                return Ok(new { message = "User deleted successfully." });
            return BadRequest(new { message = result.ErrorMessage });
        }
        [HttpGet("payment-statistics")]
        public async Task<IActionResult> GetPaymentStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddMonths(-1);
                var statistics = await _userManagementService.GetPaymentStatisticsAsync(start, endDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, "An error occurred while retrieving payment statistics");
            }
        }

        [HttpGet("download-user-statistics")]
        public async Task<IActionResult> DownloadUserStatistics([FromQuery] DateTime? startDate = null)
        {
            try
            {
                // Convert DateTime to an appropriate time range string
                string timeRange = "30d"; // Default to 30 days

                if (startDate.HasValue)
                {
                    var daysDifference = (DateTime.Now - startDate.Value).TotalDays;

                    if (daysDifference <= 7)
                        timeRange = "7d";
                    else if (daysDifference <= 30)
                        timeRange = "30d";
                    else if (daysDifference <= 90)
                        timeRange = "90d";
                    else
                        timeRange = "1y";
                }

                // Call the service method with the string parameter
                var statistics = await _userManagementService.GetUserStatisticsAsync(timeRange, "all");

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Statistic,Value");
                csv.AppendLine($"Total Users,{statistics.TotalRegisteredUsers}");
                csv.AppendLine($"Subscribed Users,{statistics.TotalSubscribedUsers}");
                csv.AppendLine($"Regular Users,{statistics.TotalRegisteredUsers - statistics.TotalSubscribedUsers}");
                csv.AppendLine($"Total Amount Paid,{statistics.TotalAmountPaidByUsers}");

                csv.AppendLine("\nUser Growth Data");
                csv.AppendLine("Month,Total Users,New Users");
                foreach (dynamic data in statistics.UserGrowthData)
                {
                    csv.AppendLine($"{data.Name},{data.Users},{data.NewUsers}");
                }

                csv.AppendLine("\nRevenue Data");
                csv.AppendLine("Month,Total Revenue,Daily Subscriptions,Weekly Subscriptions,Monthly Subscriptions");
                foreach (dynamic data in statistics.RevenueData)
                {
                    csv.AppendLine($"{data.Name},{data.Revenue},{data.DailySubscriptions},{data.WeeklySubscriptions},{data.MonthlySubscriptions}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"user-statistics-{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error downloading user statistics");
                return StatusCode(500, "An error occurred while downloading user statistics");
            }
        }

        [HttpGet("download-payment-statistics")]
        public async Task<IActionResult> DownloadPaymentStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddMonths(-1);
                var statistics = await _userManagementService.GetPaymentStatisticsAsync(start, endDate);

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Statistic,Value");
                csv.AppendLine($"Total Transactions,{statistics.TotalTransactions}");
                csv.AppendLine($"Successful Transactions,{statistics.SuccessfulTransactions}");
                csv.AppendLine($"Failed Transactions,{statistics.FailedTransactions}");
                csv.AppendLine($"Total Amount,{statistics.TotalAmount}");
                csv.AppendLine($"Successful Amount,{statistics.SuccessfulAmount}");

                csv.AppendLine("\nDaily Transaction Summary");
                csv.AppendLine("Date,Total Count,Successful Count,Failed Count,Total Amount,Successful Amount");
                foreach (var data in statistics.DailyTransactions)
                {
                    csv.AppendLine($"{data.Date:yyyy-MM-dd},{data.TotalCount},{data.SuccessfulCount},{data.FailedCount},{data.TotalAmount},{data.SuccessfulAmount}");
                }

                csv.AppendLine("\nFailed Transaction Details");
                csv.AppendLine("Customer Reference,Phone Number,Amount,Transaction Date,Response Code");
                foreach (var data in statistics.FailedTransactionDetails)
                {
                    csv.AppendLine($"{data.CustomerRef},{data.PhoneNumber},{data.Amount},{data.TransactionDate:yyyy-MM-dd HH:mm:ss},{data.ResponseCode}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"payment-statistics-{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, "An error occurred while downloading payment statistics");
            }
        }

    }
}

