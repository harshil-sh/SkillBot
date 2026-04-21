using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTelegramTokenFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramBotToken",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelegramBotToken",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }
    }
}
