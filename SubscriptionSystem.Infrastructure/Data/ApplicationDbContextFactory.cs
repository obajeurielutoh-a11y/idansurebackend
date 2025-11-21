using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;
using System;

namespace SubscriptionSystem.Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Load .env file
            var envPath = FindDotEnv();
            if (!string.IsNullOrEmpty(envPath))
            {
                DotNetEnv.Env.Load(envPath);
                Console.WriteLine($"[Design Time] Loaded .env from: {envPath}");
            }

            // Get connection string from environment
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__IdanSurestSecurityConnectionForPrediction");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ConnectionStrings__IdanSurestSecurityConnectionForPrediction' not found in environment variables or .env file.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 5,
                                         maxRetryDelay: TimeSpan.FromSeconds(10),
                                         errorCodesToAdd: null);
                sql.CommandTimeout(60);
            });

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Search current and parent directories for .env file
        /// </summary>
        private string? FindDotEnv()
        {
            var dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, ".env");
                if (File.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
