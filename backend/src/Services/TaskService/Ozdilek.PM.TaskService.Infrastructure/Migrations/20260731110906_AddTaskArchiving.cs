using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.TaskService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_task_items_GroupId",
                table: "task_items");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAtUtc",
                table: "task_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_items_GroupId_ArchivedAtUtc",
                table: "task_items",
                columns: new[] { "GroupId", "ArchivedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_task_items_GroupId_ArchivedAtUtc",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "task_items");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_GroupId",
                table: "task_items",
                column: "GroupId");
        }
    }
}
