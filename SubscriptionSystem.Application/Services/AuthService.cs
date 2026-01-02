using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Google.Apis.Auth;
using OtpNet;
using SubscriptionSystem.Application.Common;
namespace SubscriptionSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthService(
         IConfiguration configuration,
         IEmailService emailService,
         IUserRepository userRepository,
         ITokenService tokenService,
         IRefreshTokenService refreshTokenService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<ServiceResult<bool>> SetPasswordForSocialUserAsync(string userId, SetPasswordForSocialUserDto request)
        {
            try
            {
                // Get the user
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found"
                    };
                }

                // Check if this is a social user (has GoogleId)
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "This operation is only available for social login users"
                    };
                }

                // Hash the password using your existing HashPassword method
                string passwordHash = HashPassword(request.Password);

                // Update the user with the new password
                user.PasswordHash = passwordHash;

                // Update the AuthProvider field if you have one
                // user.AuthProvider = "Multiple"; // Or keep as "Google" but track that they have a password

                await _userRepository.UpdateUserAsync(user);

                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true
                };
            }
            catch (Exception ex)
            {
                // You might want to log the exception here
                // _logger.LogError(ex, "Error setting password for social user");

                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while setting the password"
                };
            }
        }


        // Update your SignInWithGoogleAsync method to handle IdToken
        // Modify your existing SignInWithGoogleAsync method to include a flag indicating if the user needs to set a password
        public async Task<ServiceResult<SignInResponseDto>> SignInWithGoogleAsync(GoogleSignInDto googleSignInDto)
        {
            try
            {
                // If IdToken is provided, validate it and extract user info
                if (!string.IsNullOrEmpty(googleSignInDto.IdToken))
                {
                    try
                    {
                        var settings = new GoogleJsonWebSignature.ValidationSettings
                        {
                            Audience = new[] { _configuration["Authentication:Google:ClientId"] }
                        };

                        var payload = await GoogleJsonWebSignature.ValidateAsync(googleSignInDto.IdToken, settings);

                        // Update the DTO with information from the token
                        googleSignInDto.Email = payload.Email;
                        googleSignInDto.FullName = payload.Name;
                        googleSignInDto.GoogleId = payload.Subject;
                        googleSignInDto.ProfilePicture = payload.Picture;
                    }
                    catch (InvalidJwtException)
                    {
                        return new ServiceResult<SignInResponseDto>
                        {
                            IsSuccess = false,
                            ErrorMessage = "Invalid Google token."
                        };
                    }
                }

                // Check if user exists with the given email or GoogleId
                var user = await _userRepository.GetUserByEmailAsync(googleSignInDto.Email);
                if (user == null && !string.IsNullOrEmpty(googleSignInDto.GoogleId))
                {
                    // Try to find by GoogleId if you have this method
                    user = await _userRepository.GetUserByGoogleIdAsync(googleSignInDto.GoogleId);
                }

                bool isNewUser = false;

                if (user == null)
                {
                    // Create a new user if they don't exist
                    user = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = googleSignInDto.Email,
                        FullName = googleSignInDto.FullName ?? "Google User", // Provide default if null
                        PhoneNumber = string.Empty,
                        PasswordHash = string.Empty,
                        Role = "User",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        GoogleId = googleSignInDto.GoogleId,
                        ProfilePicture = googleSignInDto.ProfilePicture,
                        // Check if these fields are required in your User entity:
                    };

                    await _userRepository.CreateUserAsync(user);
                    isNewUser = true; // Mark as new user
                }
                else
                {
                    // Update existing user's Google ID if not set
                    if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = googleSignInDto.GoogleId;
                        user.ProfilePicture = googleSignInDto.ProfilePicture;

                        // If the user doesn't have a name yet, use the one from Google
                        if (string.IsNullOrEmpty(user.FullName))
                        {
                            user.FullName = googleSignInDto.FullName;
                        }

                        await _userRepository.UpdateUserAsync(user);
                    }
                }

                // Check if the user has a password set
                bool needsPassword = string.IsNullOrEmpty(user.PasswordHash);

                // Complete the sign-in process
                var signInResult = await CompleteSignInAsync(user);

                // Add the new flags to the response
                if (signInResult.IsSuccess && signInResult.Data != null)
                {
                    signInResult.Data.IsNewUser = isNewUser;
                    signInResult.Data.NeedsPassword = needsPassword;
                }

                return signInResult;
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ServiceResult<SignInResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred during Google sign-in: {ex.Message}"
                };
            }
        }







        public async Task<ServiceResult<bool>> UpdateFullNameAsync(string email, string fullName)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
                }

                user.FullName = fullName;
                await _userRepository.UpdateUserAsync(user);

                return new ServiceResult<bool> { IsSuccess = true, Data = true };
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = $"An error occurred while updating the full name: {ex.Message}" };
            }
        }

        public async Task<Result<UserSubscriptionResponseDto>> CheckUserSubscriptionAsync(string customerRef)
        {
            try
            {
                // Normalize the CustomerRef (e.g., convert "07030600366" to "+2347030600366")
                string normalizedCustomerRef = NormalizeCustomerRef(customerRef);

                // Fetch the user by the normalized CustomerRef
                var user = await _userRepository.GetUserByPhoneNumberAsync(normalizedCustomerRef);

                if (user == null)
                {
                    return Result<UserSubscriptionResponseDto>.Failure("User not found.");
                }

                return Result<UserSubscriptionResponseDto>.Success(new UserSubscriptionResponseDto
                {
                    TraceId = Guid.NewGuid().ToString("N"),
                    CustomerName = user.FullName,
                    Amount = user.SubscriptionAmount,
                    DisplayMessage = $"{user.FullName} Subscription Purchase",
                    ResponseCode = "00" // Success
                });
            }
            catch (ArgumentException ex)
            {
                return Result<UserSubscriptionResponseDto>.Failure(ex.Message);
            }
        }

        private string NormalizeCustomerRef(string customerRef)
        {
            if (string.IsNullOrEmpty(customerRef))
                throw new ArgumentException("CustomerRef is required.");

            if (customerRef.StartsWith("0"))
                return "+234" + customerRef.Substring(1); // Convert "07030600366" → "+2347030600366"

            if (customerRef.StartsWith("+234"))
                return customerRef;

            throw new ArgumentException("Invalid CustomerRef format.");
        }

     
        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.", nameof(email));

            return await _userRepository.GetUserByEmailAsync(email) ?? throw new Exception("User not found.");
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            return await _userRepository.GetUserByIdAsync(userId) ?? throw new Exception("User not found.");
        }

    

        public async Task<ServiceResult<SignUpResponseDto>> SignUpAsync(UserSignUpDto signUpData)
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(signUpData.Email);
            if (existingUser != null)
            {
                return new ServiceResult<SignUpResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = "User with this email already exists."
                };
            }

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = signUpData.Email,
                PhoneNumber = signUpData.PhoneNumber,
                FullName = signUpData.FullName,
                DateOfBirth = signUpData.DateOfBirth,
                PasswordHash = HashPassword(signUpData.Password),
                Role = "User",
                IsActive = true, // User is immediately active, no OTP verification needed
                CreatedAt = DateTime.UtcNow,
                AccountDeletionRequested = false
            };

            newUser.TwoFactorSecret = GenerateTwoFactorSecret();
            newUser.TwoFactorEnabled = false;

            await _userRepository.CreateUserAsync(newUser);

            // Generate tokens immediately
            var token = GenerateJwtToken(newUser);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(newUser.Id, "system", TimeSpan.FromDays(_configuration.GetValue<int>("Auth:RefreshTokenExpiryDays", 7)));

            // Keep legacy user fields null/empty; refresh tokens are stored in RefreshTokens table
            newUser.RefreshToken = null;
            newUser.RefreshTokenExpiryTime = null;
            await _userRepository.UpdateUserAsync(newUser);

            // Send welcome email instead of OTP
            await SendWelcomeEmail(newUser.Email);

            // Return the signup response with tokens
            return new ServiceResult<SignUpResponseDto>
            {
                IsSuccess = true,
                Data = new SignUpResponseDto
                {
                    UserId = newUser.Id,
                    Email = newUser.Email,
                    PhoneNumber = newUser.PhoneNumber,
                    FullName = newUser.FullName,
                    Role = newUser.Role,
                    IsActive = newUser.IsActive,
                    Token = token,
                    RefreshToken = refreshToken,
                    Message = "User registered successfully. You can now sign in with your credentials."
                }
            };
        }

        public async Task<ServiceResult<bool>> SignOutAsync(string userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found."
                    };
                }

                // Revoke the refresh token (if using refresh tokens)
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                // Save changes to the database
                await _userRepository.UpdateUserAsync(user);

                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "User signed out successfully."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred while signing out: {ex.Message}"
                };
            }
        }


        public async Task<ServiceResult<bool>> ConfirmEmailAsync(EmailConfirmationDto confirmationData)
        {
            var user = await _userRepository.GetUserByEmailAsync(confirmationData.Email);
            if (user == null || user.EmailConfirmationOTP != confirmationData.OTP || user.EmailConfirmationOTPExpiry < DateTime.UtcNow)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Invalid or expired OTP." };
            }

            user.IsActive = true;
            user.EmailConfirmationOTP = null;
            user.EmailConfirmationOTPExpiry = null;
            await _userRepository.UpdateUserAsync(user);

            await SendWelcomeEmail(user.Email);
            await SendEmailConfirmationSuccessEmail(user.Email);

            return new ServiceResult<bool> { IsSuccess = true, Data = true, Message = "Email confirmed successfully. You can now sign in." };
        }

        public async Task<ServiceResult<SignInResponseDto>> SignInAsync(UserSignInDto signInData)
        {
            try
            {
                // Validate that at least one identifier is provided
                if (string.IsNullOrWhiteSpace(signInData.Email) && string.IsNullOrWhiteSpace(signInData.PhoneNumber))
                {
                    return new ServiceResult<SignInResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Either email or phone number must be provided"
                    };
                }

                // Try to find the user by email or phone number
                User user = null;

                if (!string.IsNullOrWhiteSpace(signInData.Email))
                {
                    // Find user by email
                    user = await _userRepository.GetUserByEmailAsync(signInData.Email);
                }
                else if (!string.IsNullOrWhiteSpace(signInData.PhoneNumber))
                {
                    // Find user by phone number
                    user = await _userRepository.GetUserByPhoneNumberAsync(signInData.PhoneNumber);
                }

                // If user not found or password is invalid
                if (user == null || !VerifyPassword(signInData.Password, user.PasswordHash))
                {
                    return new ServiceResult<SignInResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid credentials"
                    };
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    return new ServiceResult<SignInResponseDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Your account is not active. Please contact support."
                    };
                }

                // Handle two-factor authentication if enabled
                if (user.TwoFactorEnabled)
                {
                    // Return a partial sign-in response, indicating 2FA is required
                    return new ServiceResult<SignInResponseDto>
                    {
                        IsSuccess = true,
                        Data = new SignInResponseDto
                        {
                            RequiresTwoFactor = true,
                            UserId = user.Id,
                            Email = user.Email,
                            Role = user.Role,
                            HasActiveSubscription = user.HasActiveSubscription, // You might want to check this dynamically
                            Token = null, // No token is generated at this point
                            RefreshToken = null, // No refresh token is generated at this point
                            PhoneNumber = user.PhoneNumber,
                            IsNewUser = false,
                            NeedsPassword = false
                        }
                    };
                }

                // Complete the sign-in process
                return await CompleteSignInAsync(user);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ServiceResult<SignInResponseDto>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred during sign-in: {ex.Message}"
                };
            }
        }
        public async Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            // Validate provided refresh token against stored hashed tokens
            var validated = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
            if (!validated.Success || string.IsNullOrEmpty(validated.UserId))
            {
                return new ServiceResult<RefreshTokenResponseDto> { IsSuccess = false, ErrorMessage = "Invalid refresh token." };
            }

            var user = await _userRepository.GetUserByIdAsync(validated.UserId);
            if (user == null)
            {
                return new ServiceResult<RefreshTokenResponseDto> { IsSuccess = false, ErrorMessage = "Invalid refresh token." };
            }

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, "system", TimeSpan.FromDays(_configuration.GetValue<int>("Auth:RefreshTokenExpiryDays", 7)));

            // Revoke the old refresh token
            await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken, "system");

            return new ServiceResult<RefreshTokenResponseDto>
            {
                IsSuccess = true,
                Data = new RefreshTokenResponseDto
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken
                }
            };
        }

        public async Task<ServiceResult<bool>> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = true, Data = true };
            }

            string otp = GenerateOTP();
            user.PasswordResetOTP = otp;
            user.PasswordResetOTPExpiry = DateTime.UtcNow.AddMinutes(15);
            await _userRepository.UpdateUserAsync(user);

            await SendPasswordResetEmail(email, otp);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(resetPasswordDto.Email);
            if (user == null || user.PasswordResetOTP != resetPasswordDto.OTP || user.PasswordResetOTPExpiry < DateTime.UtcNow)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Invalid or expired OTP." };
            }

            user.PasswordHash = HashPassword(resetPasswordDto.NewPassword);
            user.PasswordResetOTP = null;
            user.PasswordResetOTPExpiry = null;
            await _userRepository.UpdateUserAsync(user);

            await SendPasswordResetConfirmationEmail(user.Email);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }


        public async Task<ServiceResult<bool>> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Current password is incorrect." };
            }

            user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
            await _userRepository.UpdateUserAsync(user);

            await SendPasswordChangeConfirmationEmail(user.Email);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<ServiceResult<bool>> RequestEmailChangeAsync(string userId, string newEmail)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            if (await _userRepository.GetUserByEmailAsync(newEmail) != null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Email already in use." };
            }

            string otp = GenerateOTP();
            user.EmailChangeOTP = otp;
            user.EmailChangeOTPExpiry = DateTime.UtcNow.AddMinutes(15);
            user.NewEmail = newEmail;
            await _userRepository.UpdateUserAsync(user);

            await SendEmailChangeConfirmationEmail(newEmail, otp);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<ServiceResult<bool>> ConfirmEmailChangeAsync(string userId, ConfirmEmailChangeDto confirmEmailChangeDto)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            if (user.EmailChangeOTP != confirmEmailChangeDto.OTP || user.EmailChangeOTPExpiry < DateTime.UtcNow)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "Invalid or expired OTP." };
            }

            if (user.NewEmail != confirmEmailChangeDto.NewEmail)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "New email does not match the requested change." };
            }

            string oldEmail = user.Email;
            user.Email = user.NewEmail;
            user.NewEmail = null;
            user.EmailChangeOTP = null;
            user.EmailChangeOTPExpiry = null;
            await _userRepository.UpdateUserAsync(user);

            await SendEmailChangeSuccessEmail(user.Email, oldEmail);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }


        public async Task<ServiceResult<bool>> RequestAccountDeletionAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResult<bool> { IsSuccess = false, ErrorMessage = "User not found." };
            }

            user.AccountDeletionRequested = true;
            user.AccountDeletionRequestDate = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            await SendAccountDeletionRequestEmail(user.Email);

            return new ServiceResult<bool> { IsSuccess = true, Data = true };
        }

        public async Task<ServiceResult<SignInResponseDto>> VerifyTwoFactorAsync(string userId, string twoFactorCode)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || !user.TwoFactorEnabled)
            {
                return new ServiceResult<SignInResponseDto> { IsSuccess = false, ErrorMessage = "Invalid user or 2FA not enabled." };
            }

            if (!VerifyTwoFactorCode(user.TwoFactorSecret, twoFactorCode))
            {
                return new ServiceResult<SignInResponseDto> { IsSuccess = false, ErrorMessage = "Invalid 2FA code." };
            }

            return await CompleteSignInAsync(user);
        }



        public async Task<ServiceResult<bool>> VerifyOtpResetAsync(string email, string otp)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found."
                    };
                }

                // Check if OTP exists and is valid
                if (string.IsNullOrEmpty(user.PasswordResetOTP) ||
                    user.PasswordResetOTPExpiry == null ||
                    user.PasswordResetOTPExpiry < DateTime.UtcNow)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "OTP has expired or is invalid. Please request a new one."
                    };
                }

                // Verify the OTP
                if (user.PasswordResetOTP != otp)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid OTP. Please try again."
                    };
                }

              
                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "OTP verified successfully."
                };
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while verifying the OTP."
                };
            }
        }

        public async Task<ServiceResult<SignInResponseDto>> CompleteSignInAsync(User user)
        {
            var token = GenerateJwtToken(user);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, "system", TimeSpan.FromDays(_configuration.GetValue<int>("Auth:RefreshTokenExpiryDays", 7)));

            // Keep legacy user fields empty; refresh tokens stored in RefreshTokens table
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userRepository.UpdateUserAsync(user);

            var hasActiveSubscription = await _userRepository.HasActiveSubscriptionAsync(user.Id);

           

            return new ServiceResult<SignInResponseDto>
            {
                IsSuccess = true,
                Data = new SignInResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Role = user.Role,
                    HasActiveSubscription = hasActiveSubscription,
                    Token = token,
                    FullName = user.FullName,
                    RefreshToken = refreshToken,
                    RequiresTwoFactor = false,
                    PhoneNumber = user.PhoneNumber,
                }
            };
        }
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                // Mark token type explicitly for downstream policy selection
                new Claim("token_type", "user"),



            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private string GenerateOTP()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyTwoFactorCode(string secret, string code)
        {
            var base32Bytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(base32Bytes);
            return totp.VerifyTotp(code, out long timeStepMatched);
        }

        public string GenerateTwoFactorSecret()
        {
            var secret = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secret);
        }

        private string GeneratePasswordResetToken()
        {
            return Guid.NewGuid().ToString();
        }


        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private async Task SendOtpConfirmationEmail(string email, string otp)
        {
            string subject = "Confirm Your Email - IdanSure";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Confirm Your Email - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Welcome to IdanSure!</h2>
                                <p>Thank you for signing up. To complete your registration, please use the following OTP to confirm your email:</p>
                                <div style='background-color: #e9ecef; padding: 10px; text-align: center; font-size: 24px; font-weight: bold; margin: 20px 0;'>
                                    {otp}
                                </div>
                                <p>This OTP will expire in 15 minutes.</p>
                                <p>If you didn't request this, please ignore this email.</p>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendWelcomeEmail(string email)
        {
            string subject = "Welcome to IdanSure!";
            string body = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Welcome to IdanSure!</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                <tr>
                    <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                    </td>
                </tr>
                <tr>
                    <td style='background-color: #f8f9fa; padding: 20px;'>
                        <h2 style='color: #0056b3;'>Welcome to IdanSure!</h2>
                        <p>We're excited to have you on board. Your account has been created successfully.</p>
                        <p>Please confirm your email to start using our services.</p>
                        <p>If you have any questions, feel free to contact our support team.</p>
                        <div style='text-align: center; margin-top: 20px;'>
                            <a href='https://idansure.com/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Go to Login</a>
                        </div>
                    </td>
                </tr>
                <tr>
                    <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                        <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }



        private async Task SendEmailConfirmationSuccessEmail(string email)
        {
            string subject = "Email Confirmed - IdanSure";
            string body = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Email Confirmed - IdanSure</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                <tr>
                    <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                    </td>
                </tr>
                <tr>
                    <td style='background-color: #f8f9fa; padding: 20px;'>
                        <h2 style='color: #0056b3;'>Email Confirmed Successfully!</h2>
                        <p>Your email has been confirmed. You can now enjoy all the features of IdanSure.</p>
                        <p>Thank you for choosing our service!</p>
                        <div style='text-align: center; margin-top: 20px;'>
                            <a href='https://idansure.com/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Your Account</a>
                        </div>
                    </td>
                </tr>
                <tr>
                    <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                        <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }



        private async Task SendSignInNotificationEmail(string email)
        {
            string subject = "New Sign In - IdanSure";
            string body = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>New Sign In - IdanSure</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                <tr>
                    <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                    </td>
                </tr>
                <tr>
                    <td style='background-color: #f8f9fa; padding: 20px;'>
                        <h2 style='color: #0056b3;'>New Sign In Detected</h2>
                        <p>We detected a new sign in to your IdanSure account.</p>
                        <p>If this was you, no further action is needed.</p>
                        <p>If you didn't sign in, please contact our support team immediately.</p>
                        <div style='text-align: center; margin-top: 20px;'>
                            <a href='https://idansure.com/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Your Account</a>
                        </div>
                    </td>
                </tr>
                <tr>
                    <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                        <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }


        private async Task SendPasswordResetEmail(string email, string otp)
        {
            string subject = "Password Reset Request - IdanSure";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Reset - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Password Reset Request</h2>
                                <p>We received a request to reset your password. Please use the following OTP to reset your password:</p>
                                <div style='background-color: #e9ecef; padding: 10px; text-align: center; font-size: 24px; font-weight: bold; margin: 20px 0;'>
                                    {otp}
                                </div>
                                <p>This OTP will expire in 15 minutes.</p>
                                <p>If you didn't request this password reset, please ignore this email or contact our support team if you have concerns.</p>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendPasswordResetConfirmationEmail(string email)
        {
            string subject = "Password Reset Successful - IdanSure";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Reset Successful - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Password Reset Successful</h2>
                                <p>Your password has been successfully reset.</p>
                                <p>If you did not initiate this password reset, please contact our support team immediately.</p>
                                <div style='text-align: center; margin-top: 20px;'>
                            <a href='https://idansure.com/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Your Account</a>
                        </div>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendPasswordChangeConfirmationEmail(string email)
        {
            string subject = "Password Changed - IdanSure";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Changed - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Password Changed Successfully</h2>
                                <p>Your password has been successfully changed.</p>
                                <p>If you did not initiate this password change, please contact our support team immediately.</p>
                                 <div style='text-align: center; margin-top: 20px;'>
                            <a href='https://idansure.com/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Your Account</a>
                        </div>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailChangeConfirmationEmail(string email, string otp)
        {
            string subject = "Confirm Email Change - IdanSure";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Confirm Email Change - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Confirm Your New Email</h2>
                                <p>We received a request to change your email address. Please use the following OTP to confirm your new email:</p>
                                <div style='background-color: #e9ecef; padding: 10px; text-align: center; font-size: 24px; font-weight: bold; margin: 20px 0;'>
                                    {otp}
                                </div>
                                <p>This OTP will expire in 15 minutes.</p>
                                <p>If you didn't request this email change, please ignore this email or contact our support team if you have concerns.</p>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailChangeSuccessEmail(string newEmail, string oldEmail)
        {
            string subject = "Email Change Successful - IdanSure";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Email Change Successful - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Email Change Successful</h2>
                                <p>Your email has been successfully changed from {oldEmail} to {newEmail}.</p>
                                <p>If you did not initiate this email change, please contact our support team immediately.</p>
                                <div style='text-align: center; margin-top: 20px;'>
                            <a href='https://idansure.com/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Your Account</a>
                        </div>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(newEmail, subject, body);

            // Send notification to old email
            string oldEmailSubject = "Your Email Has Been Changed - IdanSure";
            string oldEmailBody = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Your Email Has Been Changed - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Your Email Has Been Changed</h2>
                                <p>This is to inform you that the email associated with your IdanSure account has been changed from {oldEmail} to {newEmail}.</p>
                                <p>If you did not initiate this email change, please contact our support team immediately.</p>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(oldEmail, oldEmailSubject, oldEmailBody);
        }

        private async Task SendAccountDeletionRequestEmail(string email)
        {
            string subject = "Account Deletion Request - IdanSure";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Account Deletion Request - IdanSure</title>
                </head>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='background-color: #0056b3; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0;'>IdanSure</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f8f9fa; padding: 20px;'>
                                <h2 style='color: #0056b3;'>Account Deletion Request Received</h2>
                                <p>We have received your request to delete your IdanSure account. Our team will process your request within the next 7 days.</p>
                                <p>During this time, you can still log in to your account. If you change your mind, you can cancel the deletion request by logging in and visiting your account settings.</p>
                                <p>If you did not initiate this account deletion request, please contact our support team immediately.</p>
                                 <div style='text-align: center; margin-top: 20px;'>
                            <a href='https://idansure.com/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Your Account</a>
                        </div>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #0056b3; color: #ffffff; text-align: center; padding: 10px;'>
                                <p style='margin: 0;'>&copy; 2023 IdanSure. All rights reserved.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}

