using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pomodoro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleModeMetadataToPomodoros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddedSeconds",
                table: "pomodoros",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "pomodoros",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedDurationSeconds",
                table: "pomodoros",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedSeconds",
                table: "pomodoros");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "pomodoros");

            migrationBuilder.DropColumn(
                name: "PlannedDurationSeconds",
                table: "pomodoros");
        }
    }
}
