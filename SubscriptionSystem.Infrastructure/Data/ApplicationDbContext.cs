using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Domain.Entities.SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Prediction> Predictions { get; set; }
        public DbSet<VerifiedEmail> VerifiedEmails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<AsedeyhotPrediction> AsedeyhotPredictions { get; set; }
        public DbSet<AccountDeletionRequest> AccountDeletionRequests { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PaymentRecord> PaymentRecords { get; set; }
        public DbSet<TransactionQueryRecord> TransactionQueryRecords { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<StandardizedTransaction> Transactions { get; set; }
        public DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                // Monetary precision
                entity.Property(e => e.SubscriptionAmount).HasPrecision(18, 2);
                entity.HasMany(u => u.Subscriptions)
                    .WithOne(s => s.User)
                    .HasForeignKey(s => s.UserId);
                entity.HasMany(u => u.Payments)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserId);
                entity.HasMany(u => u.Tickets)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Prediction>(entity =>
            {
                entity.OwnsOne(p => p.Team1Performance);
                entity.OwnsOne(p => p.Team2Performance);
                entity.Property(p => p.NonAlphanumericDetails).IsRequired(false);
            });
            modelBuilder.Entity<StandardizedTransaction>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<StandardizedTransaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Subscription>()
               .HasOne(s => s.User)
               .WithMany(u => u.Subscriptions)
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.Property(s => s.AmountPaid).HasPrecision(18, 2);
                entity.Property(s => s.TotalAmountPaid).HasPrecision(18, 2);
            });


            modelBuilder.Entity<VerifiedEmail>(entity =>
            {
                entity.HasIndex(ve => ve.Email).IsUnique();
            });

            modelBuilder.Entity<AsedeyhotPrediction>(entity =>
            {
                entity.ToTable("AsedeyhotPredictions");
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(100);
                entity.Property(e => e.MessageBody).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Tickets)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<AccountDeletionRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.RequestDate).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.Expires).IsRequired();
                entity.Property(rt => rt.IsRevoked).HasDefaultValue(false);
                entity.Property(rt => rt.IsUsed).HasDefaultValue(false);
                entity.Property(rt => rt.CreatedAt).IsRequired();

                entity.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<PaymentRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerRef).IsRequired();
                entity.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
                
                entity.Property(e => e.TransactionDate).IsRequired();
                entity.Property(e => e.PaymentReference).IsRequired();
                entity.Property(e => e.ResponseCode).IsRequired();
               
            });

            modelBuilder.Entity<TransactionQueryRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TraceId).IsRequired();
                entity.Property(e => e.CustomerRef).IsRequired();
                entity.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.Currency).IsRequired();
                entity.Property(e => e.TransactionDate).IsRequired();
                entity.Property(e => e.ResponseCode).IsRequired();
                entity.Property(e => e.ResponseMessage).IsRequired();

            });
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasPrecision(18, 2);
            });
            modelBuilder.Entity<ApiKey>()
                 .HasOne(ak => ak.User)
                 .WithMany(u => u.ApiKeys)
                 .HasForeignKey(ak => ak.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApiKey>()
                .HasIndex(ak => ak.Key)
                .IsUnique();

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
            });


        }
    }
}

