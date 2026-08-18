using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.ProjectService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTemplateBuilderSchemaFunctional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "template_fields",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "template_fields",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "SystemKey",
                table: "template_fields",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(
                """UPDATE template_fields SET "Kind" = 1 WHERE lower("ContentType") IN ('section', 'bölüm');""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "template_fields");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "template_fields");

            migrationBuilder.DropColumn(
                name: "SystemKey",
                table: "template_fields");
        }
    }
}
