using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminAndProjectManagerAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "roles_csv",
                value: "Member");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "roles_csv",
                value: "Member");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "roles_csv",
                value: "Member");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "roles_csv",
                value: "Member");

            migrationBuilder.InsertData(
                table: "employees",
                columns: new[] { "Id", "CreatedAtUtc", "DepartmentId", "DisplayName", "Email", "IsActive", "PasswordHash", "roles_csv", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111107"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Admin", "admin", true, "AQAAAAIAAYagAAAAEJwWJoSAdPaJR1mIqlm7+oh0ZK5GfExiWZ6dpEBvWRw/RAcw8mO2VHEEkcDjgm+YMg==", "Admin", "Sistem Yöneticisi", null },
                    { new Guid("11111111-1111-1111-1111-111111111108"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222202"), "Mustafa Altekin", "mustafa.altekin@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "ProjectManager", "Proje Yöneticisi", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"));

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "roles_csv",
                value: "Admin,ProjectManager,Approver");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "roles_csv",
                value: "ProjectManager,Approver");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "roles_csv",
                value: "ProjectManager");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "roles_csv",
                value: "Member,Approver");
        }
    }
}
