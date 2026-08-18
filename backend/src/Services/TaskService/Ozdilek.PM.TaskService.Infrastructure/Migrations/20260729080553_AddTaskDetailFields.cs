using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.TaskService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssigneeEmployeeId",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "task_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "task_items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueDateUtc",
                table: "task_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDateUtc",
                table: "task_items",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssigneeEmployeeId",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "DueDateUtc",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "StartDateUtc",
                table: "task_items");
        }
    }
}
