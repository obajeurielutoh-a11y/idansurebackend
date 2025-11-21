using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Collections.Generic;

namespace SubscriptionSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IConfiguration _configuration;

        public AdminController(IAdminService adminService, IConfiguration configuration)
        {
            _adminService = adminService;
            _configuration = configuration;
        }

        [HttpPost("SignUp")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SignUp([FromBody] AdminSignUpDto signUpData)
        {
            var result = await _adminService.SignUpAdminAsync(signUpData);

            if (result.IsSuccess)
                return Ok(new { message = result.Message ?? "Admin registered successfully" });
            else
                return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn([FromBody] AdminSignInDto signInData)
        {
            var result = await _adminService.SignInAdminAsync(signInData);

            if (result.IsSuccess)
            {
                var adminDetails = result.Data;
                var token = GenerateJwtToken(adminDetails);

                return Ok(new
                {
                    message = result.Message ?? "Signed in successfully",
                    token = token,
                    role = adminDetails.Role,
                    adminId = adminDetails.Id
                });
            }
            else
            {
                return Unauthorized(new { message = result.ErrorMessage });
            }
        }

    [HttpPost("CreateSuperAdmin")]
    [Authorize(AuthenticationSchemes = "AdminBearer", Policy = "AdminWithIdHeader", Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateSuperAdmin([FromBody] AdminSignUpDto signUpData)
        {
            var result = await _adminService.CreateSuperAdminAsync(signUpData);

            if (result.IsSuccess)
                return Ok(new { message = result.Message ?? "Super Admin created successfully" });
            else
                return BadRequest(new { message = result.ErrorMessage });
        }

    [HttpPut("ChangePassword")]
    [Authorize(AuthenticationSchemes = "AdminBearer", Policy = "AdminWithIdHeader", Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { message = "Invalid token" });

            var result = await _adminService.ChangePasswordAsync(adminId, changePasswordDto);

            if (result.IsSuccess)
                return Ok(new { message = result.Message ?? "Password changed successfully" });
            else
                return BadRequest(new { message = result.ErrorMessage });
        }

    [HttpPut("ResetUserPassword")]
    [Authorize(AuthenticationSchemes = "AdminBearer", Policy = "AdminWithIdHeader", Roles = "SuperAdmin")]
        public async Task<IActionResult> ResetUserPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _adminService.ResetUserPasswordAsync(resetPasswordDto);

            if (result.IsSuccess)
                return Ok(new { message = result.Message ?? "Password reset successfully" });
            else
                return BadRequest(new { message = result.ErrorMessage });
        }

        // JWT token generation method - following your existing pattern
        private string GenerateJwtToken(AdminDto admin)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, admin.Role),
                new Claim("token_type", "admin")
            };

            // Use dedicated Admin JWT settings if provided; fallback to user JWT settings for compatibility
            var adminKey = _configuration["Jwt:AdminKey"] ?? _configuration["Jwt:Key"];
            var adminIssuer = _configuration["Jwt:AdminIssuer"] ?? _configuration["Jwt:Issuer"];
            var adminAudience = _configuration["Jwt:AdminAudience"] ?? _configuration["Jwt:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: adminIssuer,
                audience: adminAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

