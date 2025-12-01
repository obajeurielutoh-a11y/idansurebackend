using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SubscriptionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyPredictionsAndDeliveryLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyPredictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Team1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Team2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PredictionOutcome = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PredictionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPredictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredictionDeliveryLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Msisdn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PredictionId = table.Column<int>(type: "integer", nullable: false),
                    SentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionDeliveryLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionDeliveryLog_DailyPredictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "DailyPredictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionDeliveryLog_PredictionId",
                table: "PredictionDeliveryLog",
                column: "PredictionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionDeliveryLog");

            migrationBuilder.DropTable(
                name: "DailyPredictions");
        }
    }
}
