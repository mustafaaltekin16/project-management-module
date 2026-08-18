using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.FeasibilityService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkPackageTimelineLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimelineSortOrder",
                table: "feasibility_main_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkPackageId",
                table: "feasibility_main_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_feasibility_main_groups_ProjectId_WorkPackageId",
                table: "feasibility_main_groups",
                columns: new[] { "ProjectId", "WorkPackageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_feasibility_main_groups_ProjectId_WorkPackageId",
                table: "feasibility_main_groups");

            migrationBuilder.DropColumn(
                name: "TimelineSortOrder",
                table: "feasibility_main_groups");

            migrationBuilder.DropColumn(
                name: "WorkPackageId",
                table: "feasibility_main_groups");
        }
    }
}
