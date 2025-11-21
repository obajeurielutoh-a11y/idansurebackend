using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdminPushSamplefixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NonAlphanumericDetails",
                table: "Predictions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NonAlphanumericDetails",
                table: "Predictions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
