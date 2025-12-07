using System;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Application.DTOs
{
    public class AdminSignInDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class AdminCreateDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        public string PhoneNumber { get; set; }
    }

    public class SuperAdminInitializeDto
    {
        [Required]
        public string InitializationKey { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(12)]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        public string PhoneNumber { get; set; }
    }

    public class AdminDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminProfileDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class ChangeAdminPasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }
    }

    public class UpdateAdminStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }

    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; }
    }

    public class LogoutDto
    {
        [Required]
        public string RefreshToken { get; set; }
    }

    public class TokenResultDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    //public class PaginatedResult<T>
    //{
    //    public List<T> Items { get; set; }
    //    public int TotalCount { get; set; }
    //    public int PageCount { get; set; }
    //    public int CurrentPage { get; set; }
    //    public int PageSize { get; set; }
    //}
}

