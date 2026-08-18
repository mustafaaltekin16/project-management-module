using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.TaskService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskCompletionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                table: "task_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedBy",
                table: "task_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE task_items
                SET "CompletedAtUtc" = COALESCE("UpdatedAtUtc", "CreatedAtUtc"),
                    "CompletedBy" = "AssigneeName"
                WHERE "Status" = 'Done';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "task_items");
        }
    }
}
