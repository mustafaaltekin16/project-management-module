using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSuggestionItemDescriptionAndSequenceNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ai_suggestion_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SequenceNote",
                table: "ai_suggestion_items",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ai_suggestion_items");

            migrationBuilder.DropColumn(
                name: "SequenceNote",
                table: "ai_suggestion_items");
        }
    }
}
