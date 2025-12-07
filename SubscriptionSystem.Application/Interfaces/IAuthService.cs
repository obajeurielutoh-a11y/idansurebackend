using SubscriptionSystem.Application.Common;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;
using System.Threading.Tasks;


namespace SubscriptionSystem.Application.Interfaces
{
    public interface IAuthService
    {
        // Existing methods...

        Task<Result<UserSubscriptionResponseDto>> CheckUserSubscriptionAsync(string customerRef);
        Task<ServiceResult<SignUpResponseDto>> SignUpAsync(UserSignUpDto signUpData);
        Task<ServiceResult<bool>> ForgotPasswordAsync(string email);
        Task<ServiceResult<bool>> VerifyOtpResetAsync(string email, string otp);
        Task<ServiceResult<bool>> UpdateFullNameAsync(string email, string fullName);
        Task<ServiceResult<bool>> RequestEmailChangeAsync(string userId, string newEmail);
        Task<ServiceResult<bool>> SetPasswordForSocialUserAsync(string userId, SetPasswordForSocialUserDto request);
        Task<ServiceResult<bool>> RequestAccountDeletionAsync(string userId);

        Task<ServiceResult<bool>> ConfirmEmailAsync(EmailConfirmationDto confirmationData);
        Task<ServiceResult<SignInResponseDto>> SignInAsync(UserSignInDto signInData);
        Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(string refreshToken);
        
        Task<ServiceResult<bool>> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<ServiceResult<SignInResponseDto>> SignInWithGoogleAsync(GoogleSignInDto googleSignInDto);
        string GenerateTwoFactorSecret();
        //Task<bool> SignOutAsync(string userId);
        //Task<ServiceResult<bool>> SignOutAsync(string userId);
        Task<ServiceResult<bool>> SignOutAsync(string userId);
        Task<User> GetUserByEmailAsync(string email);

        
        Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
      
       
        Task<ServiceResult<bool>> ConfirmEmailChangeAsync(string userId, ConfirmEmailChangeDto confirmEmailChangeDto);
       
        Task<ServiceResult<SignInResponseDto>> VerifyTwoFactorAsync(string userId, string twoFactorCode);
     
     
        Task<User> GetUserByIdAsync(string userId);
        Task<ServiceResult<SignInResponseDto>> CompleteSignInAsync(User user);




    }
}

