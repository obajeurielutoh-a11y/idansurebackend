using Microsoft.Extensions.DependencyInjection;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.Services;

namespace SubscriptionSystem.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register all your application services here
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPredictionService, PredictionService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<IAsedeyhotPredictionService, AsedeyhotPredictionService>();

            // Add any other application services here

            return services;
        }
    }
}

