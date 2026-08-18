using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedDocumentNamesToAiSuggestionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedDocumentNames",
                table: "ai_suggestion_requests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedDocumentNames",
                table: "ai_suggestion_requests");
        }
    }
}
