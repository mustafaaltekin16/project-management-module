using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.TaskService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMainTaskToTaskItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMainTask",
                table: "task_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: before this migration, "main task" was only implied by Depth == 0 (no
            // dependency). Preserve that classification for rows that already exist so historical
            // AI-approved and manually created tasks aren't reclassified as sub-tasks after upgrade.
            migrationBuilder.Sql("""UPDATE task_items SET "IsMainTask" = TRUE WHERE "Depth" = 0;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMainTask",
                table: "task_items");
        }
    }
}
