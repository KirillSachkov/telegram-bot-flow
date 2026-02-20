using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramBotFlow.Core.Data.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "bot_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    roadmap = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_bot_settings", x => x.id);
                });

            _ = migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_users", x => x.telegram_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "bot_settings");

            _ = migrationBuilder.DropTable(
                name: "users");
        }
    }
}
