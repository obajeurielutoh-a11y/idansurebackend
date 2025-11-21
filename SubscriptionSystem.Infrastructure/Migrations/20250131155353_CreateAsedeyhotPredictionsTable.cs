using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Infrastructure.Migrations
{
    public partial class CreateAsedeyhotPredictionsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsedeyhotPredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlphanumericPrediction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NonAlphanumericDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PredictedOutcome = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsPromotional = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsWin = table.Column<bool>(type: "bit", nullable: true),
                    ResultDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsedeyhotPredictions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsedeyhotPredictions");
        }
    }
}

