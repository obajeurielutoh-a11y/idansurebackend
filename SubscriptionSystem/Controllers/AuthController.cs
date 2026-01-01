using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly SubscriptionSystem.Application.Interfaces.IRefreshTokenService _refreshTokenService;

    public AuthController(IAuthService authService, IConfiguration configuration, SubscriptionSystem.Application.Interfaces.IRefreshTokenService refreshTokenService)
    {
        _authService = authService;
        _configuration = configuration;
        _refreshTokenService = refreshTokenService;
    }
    [HttpPost("set-password-for-social")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> SetPasswordForSocialUser([FromBody] SetPasswordForSocialUserDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "User ID not found in token." });
        }

        var result = await _authService.SetPasswordForSocialUserAsync(userId, request);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Password set successfully. You can now sign in with your email and password." });
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] UserSignInDto signInData)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.SignInAsync(signInData);
            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Set authentication cookies
            SetAuthCookies(result.Data.Token, result.Data.RefreshToken);

            // Return the standard response
            return Ok(new
            {
                message = "Signed in successfully",
                token = result.Data.Token,
                user = new
                {
                    id = result.Data.UserId,
                    email = result.Data.Email,
                    fullName = result.Data.FullName,
                    role = result.Data.Role,
                    hasActiveSubscription = result.Data.HasActiveSubscription,
                    phoneNumber = result.Data.PhoneNumber
                }
            });
        }
        catch (System.Exception ex)
        {
            _configuration.GetSection("Logging");
            // Log and return a friendly non-500 response to avoid empty 500s
            // Use Console and logger if available
            try { _authService?.GetType(); } catch {}
            return StatusCode(503, new { message = "Authentication temporarily unavailable", details = ex.Message });
        }
    }
    [HttpPost("CheckUserSubscription")]
    //[Authorize(AuthenticationSchemes = "Basic")]
    public async Task<IActionResult> CheckUserSubscription([FromBody] CheckUserSubscriptionRequestDto request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrEmpty(request.CustomerRef))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    responseCode = "01",
                    message = "CustomerRef is required."
                });
            }

            if (string.IsNullOrEmpty(request.MerchantId) || string.IsNullOrEmpty(request.ShortCode))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    responseCode = "01",
                    message = "MerchantId and ShortCode are required."
                });
            }

            // Call the service to check user subscription
            var result = await _authService.CheckUserSubscriptionAsync(request.CustomerRef);

            if (result == null)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    responseCode = "02",
                    message = "User subscription not found."
                });
            }

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    responseCode = "00",
                    traceId = result.Data.TraceId,
                    customerName = result.Data.CustomerName,
                    amount = result.Data.Amount,
                    displayMessage = $"{result.Data.CustomerName} Subscription Purchase"
                });
            }
            else
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    responseCode = "01",
                    message = result.ErrorMessage
                });
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                statusCode = 400,
                responseCode = "01",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                statusCode = 500,
                responseCode = "99",
                message = "Internal server error."
            });
        }
    }


    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] UserSignUpDto signUpData)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.SignUpAsync(signUpData);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        // Set authentication cookies
        SetAuthCookies(result.Data.Token, result.Data.RefreshToken);

        // Format the response to match the sign-in response format exactly
        return Ok(new
        {
            message = "Signed up successfully",
            token = result.Data.Token,
            user = new
            {
                id = result.Data.UserId,
                email = result.Data.Email,
                fullName = result.Data.FullName,
                role = result.Data.Role,
                hasActiveSubscription = false, // Default for new users
                phoneNumber = result.Data.PhoneNumber
            }
        });
    }

    [HttpPost("ConfirmEmail")]
    public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmationDto confirmationData)
    {
        var result = await _authService.ConfirmEmailAsync(confirmationData);

        if (result.IsSuccess)
            return Ok(new { message = "Email confirmed successfully. You can now sign in." });
        else
            return BadRequest(new { message = result.ErrorMessage });
    }

    [Authorize]
    [HttpPost("signout")]
    public async Task<IActionResult> SignOut()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "User ID not found in token." });
        }

        var result = await _authService.SignOutAsync(userId);

        // Clear auth cookies regardless of result
        ClearAuthCookies();

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "User signed out successfully." });
    }
  
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        // Get refresh token from cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token not found." });
        }

        var result = await _authService.RefreshTokenAsync(refreshToken);
        if (!result.IsSuccess)
        {
            // Clear cookies on error
            ClearAuthCookies();
            return BadRequest(new { message = result.ErrorMessage });
        }

        // Set new authentication cookies
        SetAuthCookies(result.Data.Token, result.Data.RefreshToken);

        return Ok(new { message = "Token refreshed successfully", token = result.Data.Token });
    }

    public class RevokeRequestDto { public string Token { get; set; } }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequestDto request)
    {
        string token = request?.Token;
        if (string.IsNullOrEmpty(token))
        {
            Request.Cookies.TryGetValue("refreshToken", out token);
        }

        if (string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Refresh token is required." });

        await _refreshTokenService.RevokeRefreshTokenAsync(token, Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        // Clear cookies
        ClearAuthCookies();

        return Ok(new { message = "Refresh token revoked." });
    }

    [HttpPost("forgot-password")]
    //[dont involve security]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        var result = await _authService.ForgotPasswordAsync(forgotPasswordDto.Email);

        // Always return success to prevent email enumeration attacks
        return Ok(new { message = "If your email is registered, you will receive a password reset OTP." });
    }
    [HttpPost("reset-password")]
    //[dont involve security]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var result = await _authService.ResetPasswordAsync(resetPasswordDto);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        // If auto-login after password reset is enabled, set auth cookies
        if (_configuration.GetValue<bool>("Auth:AutoLoginAfterPasswordReset", true))
        {
            // Get the user and generate tokens
            var user = await _authService.GetUserByEmailAsync(resetPasswordDto.Email);
            var signInResult = await _authService.CompleteSignInAsync(user);

            if (signInResult.IsSuccess)
            {
                // Set authentication cookies
                SetAuthCookies(signInResult.Data.Token, signInResult.Data.RefreshToken);

                return Ok(new
                {
                    message = "Password reset successfully. You have been automatically signed in.",
                    token = signInResult.Data.Token,
                    user = new
                    {
                        id = signInResult.Data.UserId,
                        email = signInResult.Data.Email,
                        fullName = signInResult.Data.FullName,
                        role = signInResult.Data.Role,
                        hasActiveSubscription = signInResult.Data.HasActiveSubscription,
                        phoneNumber = signInResult.Data.PhoneNumber
                    }
                });
            }
        }

        return Ok(new { message = "Password reset successfully. You can now sign in with your new password." });
    }
    
    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "User ID not found in token." });
        }

        var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        // If refreshing tokens after password change is enabled
        if (_configuration.GetValue<bool>("Auth:RefreshTokensAfterPasswordChange", true))
        {
            // Get the user and generate new tokens
            var user = await _authService.GetUserByIdAsync(userId);
            var signInResult = await _authService.CompleteSignInAsync(user);

            if (signInResult.IsSuccess)
            {
                // Set new authentication cookies
                SetAuthCookies(signInResult.Data.Token, signInResult.Data.RefreshToken);

                return Ok(new
                {
                    message = "Password changed successfully. Your session has been refreshed.",
                    token = signInResult.Data.Token
                });
            }
        }

        return Ok(new { message = "Password changed successfully." });
    }
   // Explicitly mark as not requiring authentication
    [HttpPost("SignInWithGoogle")]
    public async Task<IActionResult> SignInWithGoogle([FromBody] GoogleSignInDto googleSignInDto)
    {
        if (googleSignInDto == null || string.IsNullOrEmpty(googleSignInDto.IdToken))
        {
            
            return BadRequest(new { message = "Invalid request: Missing required fields" });
        }

       

        var result = await _authService.SignInWithGoogleAsync(googleSignInDto);

        if (result.IsSuccess)
        {
            SetAuthCookies(result.Data.Token, result.Data.RefreshToken);

          

            return Ok(new
            {
                message = "Signed in with Google successfully",
                user = new
                {
                    id = result.Data.UserId,
                    email = result.Data.Email,
                    fullName = result.Data.FullName, // Make sure this is included
                    role = result.Data.Role,
                    hasActiveSubscription = result.Data.HasActiveSubscription,
                    phoneNumber = result.Data.PhoneNumber // Include if available
                },
                token = result.Data.Token,
                refreshToken = result.Data.RefreshToken
            });
        }
        else
        {
     
            return Unauthorized(new { message = result.ErrorMessage });
        }
    }
    [HttpPost("verify-otp-reset")]
    public async Task<IActionResult> VerifyOtpReset([FromBody] VerifyOtpResetDto verifyOtpResetDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Call the service to verify the OTP
            var result = await _authService.VerifyOtpResetAsync(verifyOtpResetDto.Email, verifyOtpResetDto.Otp);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "OTP verified successfully. You can now reset your password." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while verifying the OTP." });
        }
    }
    [HttpPut("UpdateFullName")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> UpdateFullName([FromBody] UpdateFullNameDto updateFullNameDto)
    {
        if (string.IsNullOrEmpty(updateFullNameDto.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var result = await _authService.UpdateFullNameAsync(updateFullNameDto.Email, updateFullNameDto.FullName);

        if (result.IsSuccess)
            return Ok(new { message = "Full name updated successfully.", fullName = updateFullNameDto.FullName });
        else
            return BadRequest(new { message = result.ErrorMessage });
    }

    private void SetAuthCookies(string token, string refreshToken)
    {
        // Get cookie settings from configuration
        var cookieSecure = _configuration.GetValue<bool>("Auth:CookieSecure", true);
        var cookieDomain = _configuration.GetValue<string>("Auth:CookieDomain", null);
        var cookiePath = _configuration.GetValue<string>("Auth:CookiePath", "/");
        var tokenExpiry = _configuration.GetValue<int>("Auth:TokenExpiryMinutes", 60);
        var refreshTokenExpiry = _configuration.GetValue<int>("Auth:RefreshTokenExpiryDays", 2);

        // Set JWT token cookie
        Response.Cookies.Append("token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = cookieSecure, // Should be true in production
            SameSite = SameSiteMode.Strict,
            Domain = cookieDomain,
            Path = cookiePath,
            Expires = DateTime.UtcNow.AddMinutes(tokenExpiry)
        });

        // Set refresh token cookie
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = cookieSecure, // Should be true in production
            SameSite = SameSiteMode.Strict,
            Domain = cookieDomain,
            Path = cookiePath,
            Expires = DateTime.UtcNow.AddDays(refreshTokenExpiry)
        });
    }


    private void ClearAuthCookies()
    {
        var cookieDomain = _configuration.GetValue<string>("Auth:CookieDomain", null);
        var cookiePath = _configuration.GetValue<string>("Auth:CookiePath", "/");

        Response.Cookies.Delete("token", new CookieOptions
        {
            HttpOnly = true,
            Domain = cookieDomain,
            Path = cookiePath
        });

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Domain = cookieDomain,
            Path = cookiePath
        });
    }


    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { message = "Authorization code is missing" });
        }

        try
        {
            // Exchange the authorization code for tokens
            var tokenResponse = await ExchangeCodeForTokensAsync(code);

            // Get user info using the access token
            var userInfo = await GetGoogleUserInfoAsync(tokenResponse.AccessToken);

            // Create or update user in your system
            var googleSignInDto = new GoogleSignInDto
            {
                Email = userInfo.Email,
                FullName = userInfo.Name,
                GoogleId = userInfo.Id,
                ProfilePicture = userInfo.Picture
            };

            var result = await _authService.SignInWithGoogleAsync(googleSignInDto);

            if (!result.IsSuccess)
            {
                // Redirect to frontend with error
                return Redirect($"{_configuration["Frontend:BaseUrl"]}/login?error={Uri.EscapeDataString(result.ErrorMessage)}");
            }

            // Set cookies
            SetAuthCookies(result.Data.Token, result.Data.RefreshToken);

            // Redirect to frontend with success
            return Redirect($"{_configuration["Frontend:BaseUrl"]}/dashboard");
        }
        catch (Exception ex)
        {
            // Log the exception
            return Redirect($"{_configuration["Frontend:BaseUrl"]}/login?error={Uri.EscapeDataString("An error occurred during Google sign-in")}");
        }
    }

    private async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(string code)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        var redirectUri = $"{_configuration["Backend:BaseUrl"]}/api/Auth/google-callback";

        using var httpClient = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "code", code },
        { "client_id", clientId },
        { "client_secret", clientSecret },
        { "redirect_uri", redirectUri },
        { "grant_type", "authorization_code" }
    });

        var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to exchange code for tokens: {responseString}");
        }

        return JsonSerializer.Deserialize<GoogleTokenResponse>(responseString);
    }

    private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get user info: {responseString}");
        }

        return JsonSerializer.Deserialize<GoogleUserInfo>(responseString);
    }

    // Add these classes to your project
    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }

    public class GoogleUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("verified_email")]
        public bool VerifiedEmail { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; }

        [JsonPropertyName("picture")]
        public string Picture { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }
    }


}