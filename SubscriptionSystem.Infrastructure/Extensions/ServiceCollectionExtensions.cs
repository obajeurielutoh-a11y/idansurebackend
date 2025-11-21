using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionSystem.Infrastructure.Data;
using SubscriptionSystem.Infrastructure.Repositories;
using SubscriptionSystem.Infrastructure.Services;
using SubscriptionSystem.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.Services;

namespace SubscriptionSystem.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register all your infrastructure services here
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPredictionRepository, PredictionRepository>();
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IGroupChatService, GroupChatService>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IVerifiedEmailRepository, VerifiedEmailRepository>();
            services.AddScoped<IAsedeyhotPredictionRepository, AsedeyhotPredictionRepository>();


            // Add any other infrastructure services here

            // You can also configure infrastructure-specific services here
            // For example, configuring the database context:
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(configuration.GetConnectionString("IdanSurestSecurityConnectionForPrediction")));

            return services;
        }
    }
}

