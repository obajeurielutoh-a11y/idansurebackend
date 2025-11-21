using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class chatmessageswithvoicenote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoiceNoteUrl",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoiceNoteUrl",
                table: "Comments");
        }
    }
}
