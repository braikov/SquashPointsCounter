using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Squash.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentPublicIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_IsPublished",
                table: "Tournaments",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Slug",
                table: "Tournaments",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tournaments_IsPublished",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_Slug",
                table: "Tournaments");
        }
    }
}
