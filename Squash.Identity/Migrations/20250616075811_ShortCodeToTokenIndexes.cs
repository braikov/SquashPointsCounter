using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Squash.Identity.Migrations
{
    /// <inheritdoc />
    public partial class ShortCodeToTokenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ShortCodeToTokens_Email_Code",
                table: "ShortCodeToTokens",
                columns: new[] { "Email", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShortCodeToTokens_Email_Code",
                table: "ShortCodeToTokens");
        }
    }
}
