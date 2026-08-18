using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueDepartmentHead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE departments AS department
                SET "HeadEmployeeId" = NULL
                WHERE department."HeadEmployeeId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM employees AS employee
                      WHERE employee."Id" = department."HeadEmployeeId"
                        AND employee."DepartmentId" = department."Id"
                        AND employee."IsActive" = TRUE
                  );
                """);

            // No earlier migration ever created a HeadEmployeeId index (AddDepartments only added the
            // column) — nothing to drop here, just create the unique one.
            migrationBuilder.CreateIndex(
                name: "IX_departments_HeadEmployeeId",
                table: "departments",
                column: "HeadEmployeeId",
                unique: true,
                filter: "\"HeadEmployeeId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_HeadEmployeeId",
                table: "departments");
        }
    }
}
