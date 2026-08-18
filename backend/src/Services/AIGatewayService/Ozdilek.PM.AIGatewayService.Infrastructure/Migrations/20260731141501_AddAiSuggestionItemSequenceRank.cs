using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ozdilek.PM.AIGatewayService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSuggestionItemSequenceRank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SequenceRank",
                table: "ai_suggestion_items",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequenceRank",
                table: "ai_suggestion_items");
        }
    }
}
