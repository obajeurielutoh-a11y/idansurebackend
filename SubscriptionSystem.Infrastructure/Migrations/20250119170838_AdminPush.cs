using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdminPush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tournament = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Team1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Team2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatchDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NonAlphanumericDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDetailed = table.Column<bool>(type: "bit", nullable: false),
                    IsPromotional = table.Column<bool>(type: "bit", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Team1Performance_RecentWins = table.Column<int>(type: "int", nullable: false),
                    Team1Performance_RecentLosses = table.Column<int>(type: "int", nullable: false),
                    Team1Performance_AverageGoalsScored = table.Column<double>(type: "float", nullable: false),
                    Team1Performance_AverageGoalsConceded = table.Column<double>(type: "float", nullable: false),
                    Team1Performance_KeyPlayersStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Team2Performance_RecentWins = table.Column<int>(type: "int", nullable: false),
                    Team2Performance_RecentLosses = table.Column<int>(type: "int", nullable: false),
                    Team2Performance_AverageGoalsScored = table.Column<double>(type: "float", nullable: false),
                    Team2Performance_AverageGoalsConceded = table.Column<double>(type: "float", nullable: false),
                    Team2Performance_KeyPlayersStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "int", nullable: false),
                    PredictedOutcome = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailConfirmationOTP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmationOTPExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordResetOTP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetOTPExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailChangeOTP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailChangeOTPExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountDeletionRequested = table.Column<bool>(type: "bit", nullable: false),
                    AccountDeletionRequestDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerifiedEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedEmails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TotalAmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Plan = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentFailures = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedEmails_Email",
                table: "VerifiedEmails",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "VerifiedEmails");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
