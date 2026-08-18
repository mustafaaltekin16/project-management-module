using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.TaskService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkPackageTimelineLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessType",
                table: "task_groups",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimelineSortOrder",
                table: "task_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkPackageId",
                table: "task_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_groups_ProjectId_WorkPackageId_ProcessType",
                table: "task_groups",
                columns: new[] { "ProjectId", "WorkPackageId", "ProcessType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_task_groups_ProjectId_WorkPackageId_ProcessType",
                table: "task_groups");

            migrationBuilder.DropColumn(
                name: "ProcessType",
                table: "task_groups");

            migrationBuilder.DropColumn(
                name: "TimelineSortOrder",
                table: "task_groups");

            migrationBuilder.DropColumn(
                name: "WorkPackageId",
                table: "task_groups");
        }
    }
}
