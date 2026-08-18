using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.ProjectService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManagerEmployeeId",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecondManagerEmployeeId",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitDepartmentId",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "project_department_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerEmployeeId",
                table: "project_department_assignments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "SecondManagerEmployeeId",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "UnitDepartmentId",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "project_department_assignments");

            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "project_department_assignments");
        }
    }
}
