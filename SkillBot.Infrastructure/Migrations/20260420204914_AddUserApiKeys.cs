using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerpApiKey",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramBotToken",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerpApiKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramBotToken",
                table: "Users");
        }
    }
}
