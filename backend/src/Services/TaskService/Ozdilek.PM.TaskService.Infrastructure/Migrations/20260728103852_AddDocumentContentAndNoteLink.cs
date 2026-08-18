using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.TaskService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentContentAndNoteLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "project_documents",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "project_documents",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "NoteId",
                table: "project_documents",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "project_documents");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "project_documents");

            migrationBuilder.DropColumn(
                name: "NoteId",
                table: "project_documents");
        }
    }
}
