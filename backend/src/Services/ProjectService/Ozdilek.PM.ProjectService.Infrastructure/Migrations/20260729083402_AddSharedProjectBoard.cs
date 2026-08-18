using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ozdilek.PM.ProjectService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedProjectBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoardColumnId",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoardPosition",
                table: "projects",
                type: "numeric(20,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "project_board_columns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_board_columns", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "project_board_columns",
                columns: new[] { "Id", "Color", "CreatedAtUtc", "IsArchived", "Name", "SortOrder", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("70000000-0000-0000-0000-000000000001"), "#4B7DD8", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, "Yeni Projeler", 0, null },
                    { new Guid("70000000-0000-0000-0000-000000000002"), "#2F9E68", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, "Devam Edenler", 1, null },
                    { new Guid("70000000-0000-0000-0000-000000000003"), "#697386", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, "Tamamlananlar", 2, null }
                });

            migrationBuilder.Sql(
                """
                WITH ranked_projects AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "Status"
                               ORDER BY "CreatedAtUtc", "Id"
                           ) AS position
                    FROM projects
                )
                UPDATE projects AS project
                SET "BoardColumnId" = CASE
                        WHEN project."Status" = 'Draft' THEN '70000000-0000-0000-0000-000000000001'::uuid
                        WHEN project."Status" = 'Active' THEN '70000000-0000-0000-0000-000000000002'::uuid
                        ELSE '70000000-0000-0000-0000-000000000003'::uuid
                    END,
                    "BoardPosition" = ranked_projects.position * 1024
                FROM ranked_projects
                WHERE ranked_projects."Id" = project."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_projects_BoardColumnId_BoardPosition",
                table: "projects",
                columns: new[] { "BoardColumnId", "BoardPosition" });

            migrationBuilder.CreateIndex(
                name: "IX_project_board_columns_Name",
                table: "project_board_columns",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_project_board_columns_SortOrder",
                table: "project_board_columns",
                column: "SortOrder");

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_project_board_columns_active_name"
                ON project_board_columns (LOWER("Name"))
                WHERE "IsArchived" = FALSE;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_project_board_columns_BoardColumnId",
                table: "projects",
                column: "BoardColumnId",
                principalTable: "project_board_columns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_projects_project_board_columns_BoardColumnId",
                table: "projects");

            migrationBuilder.DropTable(
                name: "project_board_columns");

            migrationBuilder.DropIndex(
                name: "IX_projects_BoardColumnId_BoardPosition",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "BoardColumnId",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "BoardPosition",
                table: "projects");
        }
    }
}
