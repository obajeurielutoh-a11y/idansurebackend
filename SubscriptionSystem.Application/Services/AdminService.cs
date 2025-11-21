using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IAdminRepository _adminRepository;

        public AdminService(
            IConfiguration configuration,
            IEmailService emailService,
            IAdminRepository adminRepository)
        {
            _configuration = configuration;
            _emailService = emailService;
            _adminRepository = adminRepository;
        }

        public async Task<ServiceResult<AdminDto>> SignInAdminAsync(AdminSignInDto signInData)
        {
            try
            {
                var admin = await _adminRepository.GetAdminByEmailAsync(signInData.Email);
                if (admin == null || !VerifyPassword(signInData.Password, admin.PasswordHash))
                {
                    return new ServiceResult<AdminDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid email or password."
                    };
                }

                if (!admin.IsActive)
                {
                    return new ServiceResult<AdminDto>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Your account is not active. Please contact support."
                    };
                }

                // Return admin data for token generation
                return new ServiceResult<AdminDto>
                {
                    IsSuccess = true,
                    Data = new AdminDto
                    {
                        Id = admin.Id,
                        Email = admin.Email,
                        Role = admin.Role,
                        FullName = admin.FullName
                    },
                    Message = "Admin signed in successfully."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<AdminDto>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred during sign-in: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> SignUpAdminAsync(AdminSignUpDto signUpData)
        {
            try
            {
                // Check if admin with this email already exists
                var existingAdmin = await _adminRepository.GetAdminByEmailAsync(signUpData.Email);
                if (existingAdmin != null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Admin with this email already exists."
                    };
                }

                // Create new admin with "Admin" role
                var newAdmin = new Admin
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = signUpData.Email,
                    FullName = signUpData.Email, // Use email as name temporarily
                    PasswordHash = HashPassword(signUpData.Password),
                    Role = "Admin", // Regular admin role
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _adminRepository.CreateAdminAsync(newAdmin);

                // Send welcome email
                await SendAdminWelcomeEmail(newAdmin.Email, "Admin");

                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "Admin registered successfully."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred during admin registration: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> CreateSuperAdminAsync(AdminSignUpDto signUpData)
        {
            try
            {
                // Check if admin with this email already exists
                var existingAdmin = await _adminRepository.GetAdminByEmailAsync(signUpData.Email);
                if (existingAdmin != null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Admin with this email already exists."
                    };
                }

                // Create new admin with "SuperAdmin" role
                var newAdmin = new Admin
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = signUpData.Email,
                    FullName = signUpData.Email, // Use email as name temporarily
                    PasswordHash = HashPassword(signUpData.Password),
                    Role = "SuperAdmin", // Super admin role
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _adminRepository.CreateAdminAsync(newAdmin);

                // Send welcome email
                await SendAdminWelcomeEmail(newAdmin.Email, "SuperAdmin");

                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "Super Admin created successfully."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred during super admin creation: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> ChangePasswordAsync(string adminId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var admin = await _adminRepository.GetAdminByIdAsync(adminId);
                if (admin == null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Admin not found."
                    };
                }

                if (!VerifyPassword(changePasswordDto.CurrentPassword, admin.PasswordHash))
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Current password is incorrect."
                    };
                }

                admin.PasswordHash = HashPassword(changePasswordDto.NewPassword);
                await _adminRepository.UpdateAdminAsync(admin);

                await SendPasswordChangeConfirmationEmail(admin.Email);

                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "Password changed successfully."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred while changing password: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> ResetUserPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                // Assuming your ResetPasswordDto has Email instead of AdminId
                var admin = await _adminRepository.GetAdminByEmailAsync(resetPasswordDto.Email);
                if (admin == null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Admin not found."
                    };
                }

                admin.PasswordHash = HashPassword(resetPasswordDto.NewPassword);
                await _adminRepository.UpdateAdminAsync(admin);

                await SendPasswordResetNotificationEmail(admin.Email);

                return new ServiceResult<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Message = "Password reset successfully."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred while resetting password: {ex.Message}"
                };
            }
        }

        // Helper methods
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private async Task SendAdminWelcomeEmail(string email, string role)
        {
            string subject = $"Welcome to IdanSure as {role}";
            string body = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Welcome to IdanSure Admin</title>
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
                                <h2 style='color: #0056b3;'>Welcome to IdanSure Admin!</h2>
                                <p>You have been registered as a {role} in the IdanSure system.</p>
                                <p>You can now access the admin dashboard to manage the system.</p>
                                <div style='text-align: center; margin-top: 20px;'>
                                    <a href='https://idansure.com/admin/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Go to Admin Dashboard</a>
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
            string subject = "Admin Password Changed - IdanSure";
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
                                <h2 style='color: #0056b3;'>Admin Password Changed Successfully</h2>
                                <p>Your admin account password has been successfully changed.</p>
                                <p>If you did not initiate this password change, please contact the system administrator immediately.</p>
                                <div style='text-align: center; margin-top: 20px;'>
                                    <a href='https://idansure.com/admin/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Admin Dashboard</a>
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

        private async Task SendPasswordResetNotificationEmail(string email)
        {
            string subject = "Admin Password Reset - IdanSure";
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
                                <h2 style='color: #0056b3;'>Admin Password Reset Notification</h2>
                                <p>Your admin account password has been reset by a system administrator.</p>
                                <p>If you did not expect this change, please contact the system administrator immediately.</p>
                                <div style='text-align: center; margin-top: 20px;'>
                                    <a href='https://idansure.com/admin/login' style='background-color: #0056b3; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login to Admin Dashboard</a>
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

