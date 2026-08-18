using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.ProjectService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateValueSnapshotMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "project_template_field_values",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ListName",
                table: "project_template_field_values",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "project_template_field_values",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "project_template_field_values");

            migrationBuilder.DropColumn(
                name: "ListName",
                table: "project_template_field_values");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "project_template_field_values");
        }
    }
}
