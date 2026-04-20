using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SystemUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChannelUserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelUsers_ChannelName_ChannelUserId",
                table: "ChannelUsers",
                columns: new[] { "ChannelName", "ChannelUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelUsers_SystemUserId",
                table: "ChannelUsers",
                column: "SystemUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelUsers");
        }
    }
}
