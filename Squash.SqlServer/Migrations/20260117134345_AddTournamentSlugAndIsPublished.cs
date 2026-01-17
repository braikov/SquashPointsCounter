using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Squash.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentSlugAndIsPublished : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Tournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Tournaments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Tournaments");
        }
    }
}
