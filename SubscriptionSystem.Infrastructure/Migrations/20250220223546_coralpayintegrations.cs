using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class coralpayintegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MerchantId",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MobileNumber",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassBackReference",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "PaymentRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "MobileNumber",
                table: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "PassBackReference",
                table: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "PaymentRecords");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "PaymentRecords");
        }
    }
}
