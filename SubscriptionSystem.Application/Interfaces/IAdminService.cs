using SubscriptionSystem.Application.DTOs;
using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IAdminService
    {
        //Task<ServiceResult<string>> SignUpAdminAsync(AdminSignUpDto signUpData);
        //Task<ServiceResult<string>> SignInAdminAsync(AdminSignInDto signInData);
        Task<ServiceResult<AdminDto>> SignInAdminAsync(AdminSignInDto signInData);
        Task<ServiceResult<bool>> SignUpAdminAsync(AdminSignUpDto signUpData);
        Task<ServiceResult<bool>> CreateSuperAdminAsync(AdminSignUpDto signUpData);
        Task<ServiceResult<bool>> ChangePasswordAsync(string adminId, ChangePasswordDto changePasswordDto);
        Task<ServiceResult<bool>> ResetUserPasswordAsync(ResetPasswordDto resetPasswordDto);

    }
}

