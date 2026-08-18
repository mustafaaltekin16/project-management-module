using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "employees",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==");

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==");

            migrationBuilder.CreateIndex(
                name: "IX_employees_Email",
                table: "employees",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employees_Email",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "employees");
        }
    }
}
