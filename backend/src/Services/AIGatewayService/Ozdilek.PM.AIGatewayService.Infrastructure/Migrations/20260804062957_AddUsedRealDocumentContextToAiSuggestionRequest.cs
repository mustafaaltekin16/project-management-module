using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsedRealDocumentContextToAiSuggestionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsedRealDocumentContext",
                table: "ai_suggestion_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedRealDocumentContext",
                table: "ai_suggestion_requests");
        }
    }
}
