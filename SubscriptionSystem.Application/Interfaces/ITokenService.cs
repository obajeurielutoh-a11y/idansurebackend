namespace SubscriptionSystem.Application.Interfaces
{
    public interface ITokenService
    {
        Task<bool> ValidateRefreshTokenAsync(string email, string refreshToken);
     

    }
}
