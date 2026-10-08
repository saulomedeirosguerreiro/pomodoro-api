using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pomodoro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guest_imports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    GuestId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TasksImported = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionsImported = table.Column<int>(type: "INTEGER", nullable: false),
                    AchievementsUnlockedJson = table.Column<string>(type: "TEXT", nullable: false),
                    SkippedItemsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guest_imports_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guest_imports_UserId_GuestId",
                table: "guest_imports",
                columns: new[] { "UserId", "GuestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guest_imports");
        }
    }
}
